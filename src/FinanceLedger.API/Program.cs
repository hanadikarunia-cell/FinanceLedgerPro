using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FinanceLedger.API.Auth;
using FinanceLedger.API.Middleware;
using FinanceLedger.API.Services;
using FinanceLedger.Application;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Infrastructure;
using FinanceLedger.Infrastructure.Options;
using FinanceLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var configuration = builder.Configuration;

// ---- Options ----
builder.Services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));

// ---- Controllers / JSON ----
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<FluentValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();

// ---- API versioning ----
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Finance Ledger Pro API",
        Version = "v1",
        Description = "REST API for Finance Ledger Pro."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Example: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [securityScheme] = Array.Empty<string>()
    });
});

// ---- Authentication: Supabase Auth issues and signs every access token; the API
// only validates it (HS256 legacy shared secret) and enriches role/branch claims
// from the app_metadata Supabase embeds in the token (see SupabaseClaimsTransformation).
var supabaseSection = configuration.GetSection("Supabase");
var supabaseUrl = supabaseSection["Url"]?.TrimEnd('/') ?? string.Empty;
var supabaseJwtSecret = supabaseSection["JwtSecret"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidAudience = "authenticated",
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(supabaseJwtSecret)
                    ? "development-only-insecure-signing-key-change-me-please-32b"
                    : supabaseJwtSecret))
        };
    });

builder.Services.AddTransient<IClaimsTransformation, SupabaseClaimsTransformation>();

// ---- Authorization ----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
    options.AddPolicy("UserOrManager", policy => policy.RequireRole("User", "Manager"));
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---- Application / Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

// ---- API-layer services ----
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ILoginService, AuthService>();

// ---- CORS ----
const string CorsPolicyName = "Default";
var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// ---- Rate limiting: fixed window, 100 requests/minute per user or IP ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey =
            context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity!.Name ?? context.User.FindFirst("sub")?.Value ?? "authenticated"
                : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

// ---- Health checks: /health is a plain liveness probe (no dependency checks);
// /health/ready additionally verifies Postgres + Supabase Storage reachability.
var postgresConnectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;
builder.Services
    .AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgres", tags: new[] { "ready" })
    .AddCheck<SupabaseStorageHealthCheck>("supabase-storage", tags: new[] { "ready" });

var app = builder.Build();

// ---- Middleware pipeline ----
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Finance Ledger Pro API v1");
    });
}

app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

// ---- Seed data on startup ----
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var identityProvider = scope.ServiceProvider.GetRequiredService<IIdentityProviderService>();
        await DbInitializer.EnsureSeedDataAsync(dbContext, identityProvider, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database seeding failed on startup.");
    }
}

app.Run();
