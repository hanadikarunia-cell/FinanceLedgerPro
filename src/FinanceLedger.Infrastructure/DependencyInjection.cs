using FinanceLedger.Application.Interfaces;
using FinanceLedger.Infrastructure.Communication;
using FinanceLedger.Infrastructure.Export;
using FinanceLedger.Infrastructure.Identity;
using FinanceLedger.Infrastructure.Integration;
using FinanceLedger.Infrastructure.Options;
using FinanceLedger.Infrastructure.Persistence;
using FinanceLedger.Infrastructure.Persistence.Repositories;
using FinanceLedger.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceLedger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));
        services.Configure<ResendOptions>(configuration.GetSection("Resend"));
        services.Configure<GoogleSheetsOptions>(configuration.GetSection("GoogleSheets"));

        var postgresConnectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;
        services.AddScoped<TenantSessionInterceptor>();
        services.AddDbContext<LedgerDbContext>((sp, options) => options
            .UseNpgsql(postgresConnectionString)
            .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IImpersonationSessionRepository, ImpersonationSessionRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IPettyCashRequestRepository, PettyCashRequestRepository>();
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IReleaseNoteRepository, ReleaseNoteRepository>();

        services.AddHttpClient<IIdentityProviderService, SupabaseAuthClient>();
        services.AddHttpClient<IBlobStorageService, SupabaseStorageService>();
        services.AddHttpClient<IEmailService, ResendEmailService>();

        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IPdfExportService, PdfExportService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<IGoogleSheetsService, GoogleSheetsService>();

        return services;
    }
}
