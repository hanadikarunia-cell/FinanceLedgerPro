using FinanceLedger.Application.Interfaces;
using FinanceLedger.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceLedger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPettyCashRequestService, PettyCashRequestService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IReleaseNoteService, ReleaseNoteService>();
        services.AddScoped<ITenantService, TenantService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
