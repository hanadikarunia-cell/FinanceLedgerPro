using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Constants;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class ReportService : IReportService
{
    private readonly ITransactionRepository _repo;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public ReportService(ITransactionRepository repo, IUserService userService, ICurrentUserService currentUser)
    {
        _repo = repo;
        _userService = userService;
        _currentUser = currentUser;
    }

    public Task<ReportDto> GetDailyAsync(DateTime date, CancellationToken ct = default)
    {
        var from = date.Date;
        var to = from.AddDays(1).AddTicks(-1);
        return BuildAsync($"Daily Report - {from:yyyy-MM-dd}", from, to, ct);
    }

    public Task<ReportDto> GetMonthlyAsync(int year, int month, CancellationToken ct = default)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddTicks(-1);
        return BuildAsync($"Monthly Report - {from:yyyy-MM}", from, to, ct);
    }

    public Task<ReportDto> GetYearlyAsync(int year, CancellationToken ct = default)
    {
        var from = new DateTime(year, 1, 1);
        var to = from.AddYears(1).AddTicks(-1);
        return BuildAsync($"Yearly Report - {year}", from, to, ct);
    }

    private async Task<ReportDto> BuildAsync(string period, DateTime from, DateTime to, CancellationToken ct)
    {
        IReadOnlyCollection<string>? restrictBranches = _currentUser.IsManager ? null : _currentUser.AssignedBranches;
        var tx = await _repo.GetByDateRangeAsync(from, to, restrictBranches, ct);

        if (!_currentUser.IsManager)
            tx = tx.Where(t => t.CreatedBy == _currentUser.UserId).ToList();

        // Imprest fund model (matches DashboardService): cash leaves the Main Ledger when petty cash
        // is FUNDED, not when a User later spends it. A non-Manager viewer only ever sees their own
        // (non-Manager) transactions, so the lookup below is skipped for them.
        var managerIds = _currentUser.IsManager
            ? (await _userService.GetAllAsync(ct)).Where(u => u.Role == UserRole.Manager).Select(u => u.Id).ToHashSet()
            : new HashSet<string>();

        var income = tx.Where(t => t.Type == TransactionType.Income).ToList();
        var allExpense = tx.Where(t => t.Type == TransactionType.Expense).ToList();
        var pettyCashFunding = allExpense
            .Where(t => t.Category == TransactionCategories.PettyCash && t.ApprovalStatus == ApprovalStatus.Approved)
            .ToList();
        var pettyCashIssued = pettyCashFunding.Sum(t => t.Amount);

        // All real spending (Manager direct + User petty-cash-funded), excluding only the internal
        // funding category — this is what Managers see broken out by category to trace spending.
        var expenseByCategory = allExpense.Where(t => t.Category != TransactionCategories.PettyCash).ToList();

        // Only a Manager's own direct expenses reduce the Main Ledger balance a second time; the
        // money for a User's petty-cash-funded spending already left at funding time.
        var directManagerExpense = expenseByCategory.Where(t => managerIds.Contains(t.CreatedBy)).ToList();

        // A User's petty-cash-funded spending, once approved — mirrors DashboardService so the two
        // pages never disagree about the same underlying transactions.
        var pettyCashExpenses = expenseByCategory
            .Where(t => !managerIds.Contains(t.CreatedBy) && t.ApprovalStatus == ApprovalStatus.Approved)
            .Sum(t => t.Amount);

        // "Total Expense" is everything that has actually left the Main Ledger: a Manager's direct
        // spending plus the petty cash funding disbursed to Users — matches DashboardService exactly.
        var totalExpense = directManagerExpense.Sum(t => t.Amount) + pettyCashIssued;

        return new ReportDto
        {
            Period = period,
            From = from,
            To = to,
            TotalIncome = income.Sum(t => t.Amount),
            TotalExpense = totalExpense,
            NetBalance = income.Sum(t => t.Amount) - totalExpense,
            TotalPettyCashIssued = pettyCashIssued,
            TotalPettyCashExpenses = pettyCashExpenses,
            TotalPettyCashOutstanding = pettyCashIssued - pettyCashExpenses,
            TransactionCount = tx.Count,
            IncomeByCategory = GroupByCategory(income),
            ExpenseByCategory = GroupByCategory(expenseByCategory),
            Transactions = tx.OrderBy(t => t.Date).Select(t => t.ToDto()).ToList()
        };
    }

    private static List<CategoryTotal> GroupByCategory(IEnumerable<Transaction> tx) =>
        tx.GroupBy(t => t.Category)
          .Select(g => new CategoryTotal { Category = g.Key, Amount = g.Sum(t => t.Amount), Count = g.Count() })
          .OrderByDescending(c => c.Amount)
          .ToList();
}
