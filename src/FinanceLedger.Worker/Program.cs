using FinanceLedger.Application;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Infrastructure;
using FinanceLedger.Worker;
using FinanceLedger.Worker.Options;
using FinanceLedger.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The Worker runs outside any HTTP request, so it supplies a system principal
// for the Application services that depend on ICurrentUserService.
builder.Services.AddSingleton<SystemCurrentUserService>();
builder.Services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<SystemCurrentUserService>());
builder.Services.AddSingleton<ITenantProvider>(sp => sp.GetRequiredService<SystemCurrentUserService>());

// Shared manual-trigger signal (producer: admin endpoint/handler, consumer: worker).
builder.Services.AddSingleton<ManualSyncTrigger>();

builder.Services.AddHostedService<GoogleSheetsSyncWorker>();

var host = builder.Build();
host.Run();
