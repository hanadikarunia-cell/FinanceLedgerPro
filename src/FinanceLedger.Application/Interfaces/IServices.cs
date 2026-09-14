using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> QueryAsync(TransactionQuery query, CancellationToken ct = default);
    Task<TransactionDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TransactionDto> CreateAsync(CreateTransactionDto dto, CancellationToken ct = default);
    Task<TransactionDto> UpdateAsync(string id, UpdateTransactionDto dto, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<TransactionDto> ApproveAsync(string id, CancellationToken ct = default);
    Task<TransactionDto> RejectAsync(string id, string? reason, CancellationToken ct = default);
    Task<decimal> GetPettyCashBalanceAsync(CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
    Task<DashboardBreakdownDto> GetBreakdownAsync(string metric, CancellationToken ct = default);
}

public interface IReportService
{
    Task<ReportDto> GetDailyAsync(DateTime date, ReportQueryOptions? options = null, CancellationToken ct = default);
    Task<ReportDto> GetMonthlyAsync(int year, int month, ReportQueryOptions? options = null, CancellationToken ct = default);
    Task<ReportDto> GetYearlyAsync(int year, ReportQueryOptions? options = null, CancellationToken ct = default);
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(string id, string newPassword, CancellationToken ct = default);
}

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken ct = default);
    Task<BranchDto> CreateAsync(CreateBranchDto dto, CancellationToken ct = default);
}

public interface IPettyCashRequestService
{
    Task<PagedResult<PettyCashRequestDto>> QueryAsync(PettyCashRequestQuery query, CancellationToken ct = default);
    Task<PettyCashRequestDto> CreateAsync(CreatePettyCashRequestDto dto, CancellationToken ct = default);
    Task<PettyCashRequestDto> ApproveAsync(string id, CancellationToken ct = default);
    Task<PettyCashRequestDto> RejectAsync(string id, string? reason, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface ICarService
{
    Task<IReadOnlyList<CarDto>> GetAllAsync(CancellationToken ct = default);
    Task<CarDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<CarDto> CreateAsync(CreateCarDto dto, CancellationToken ct = default);
    Task<CarDto> UpdateAsync(string id, UpdateCarDto dto, CancellationToken ct = default);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> QueryAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto, CancellationToken ct = default);
    Task<InvoiceDto> MarkPaidAsync(string id, CancellationToken ct = default);
    Task<InvoiceDto> VoidAsync(string id, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

/// <summary>
/// One sync cycle for the Google Sheets integration: push approved transactions to
/// the configured sheet. Shared by the API's admin-triggered endpoint (the free-tier
/// path, called on a schedule by GitHub Actions) and the standalone Worker host
/// (for local use or a paid always-on deployment), so the logic exists exactly once.
/// </summary>
public interface ISyncService
{
    Task<int> SyncApprovedTransactionsAsync(CancellationToken ct = default);
}

public interface IFeedbackService
{
    Task<PagedResult<FeedbackDto>> QueryAsync(FeedbackQuery query, CancellationToken ct = default);
    Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, CancellationToken ct = default);
    Task<FeedbackDto> SetSeverityAsync(string id, FeedbackSeverity severity, CancellationToken ct = default);
}

/// <summary>The app's own "What's New" changelog — admin-authored, as opposed to
/// user-submitted Feedback. Any authenticated user can read it; only a Manager can
/// publish, edit, or delete entries.</summary>
public interface IReleaseNoteService
{
    Task<IReadOnlyList<ReleaseNoteDto>> GetAllAsync(CancellationToken ct = default);
    Task<ReleaseNoteDto> CreateAsync(CreateReleaseNoteDto dto, CancellationToken ct = default);
    Task<ReleaseNoteDto> UpdateAsync(string id, UpdateReleaseNoteDto dto, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
