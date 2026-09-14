using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Constants;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITransactionRepository _repo;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(ITransactionRepository repo, IUserService userService, ICurrentUserService currentUser)
    {
        _repo = repo;
        _userService = userService;
        _currentUser = currentUser;
    }

    // Shared by GetAsync and GetBreakdownAsync so the two never drift apart on what counts
    // toward each figure — a past bug in this codebase was exactly this kind of duplicated,
    // slowly-diverging predicate logic between Dashboard and Reports.
    private sealed class Context
    {
        public required IReadOnlyList<Transaction> All { get; init; }
        public required IReadOnlySet<string> ManagerIds { get; init; }

        public bool IsApprovedPettyCashFunding(Transaction t) =>
            t.Type == TransactionType.Expense
            && t.Category == TransactionCategories.PettyCash
            && t.ApprovalStatus == ApprovalStatus.Approved;

        // Only a Manager's own direct expenses reduce the Main Ledger balance a second time;
        // a User's petty-cash-funded spending does not (the cash already left at funding time),
        // but it is still reported as a category so Managers can trace what it was spent on.
        public bool IsDirectManagerExpense(Transaction t) =>
            t.Type == TransactionType.Expense
            && t.Category != TransactionCategories.PettyCash
            && ManagerIds.Contains(t.CreatedBy);

        // Everything that has actually left the Main Ledger: a Manager's direct expense, or
        // petty cash funding disbursed to a User.
        public bool CountsTowardTotalExpense(Transaction t) =>
            IsDirectManagerExpense(t) || IsApprovedPettyCashFunding(t);

        // A User's petty-cash-funded spending, once approved — used to compute how much of the
        // issued float each User still has outstanding (unspent) in their hands.
        public bool IsApprovedPettyCashExpense(Transaction t) =>
            t.Type == TransactionType.Expense
            && t.Category != TransactionCategories.PettyCash
            && !ManagerIds.Contains(t.CreatedBy)
            && t.ApprovalStatus == ApprovalStatus.Approved;
    }

    private async Task<Context> BuildContextAsync(CancellationToken ct)
    {
        var to = DateTime.UtcNow;
        var from = new DateTime(to.Year, to.Month, 1).AddMonths(-11);

        IReadOnlyCollection<string>? restrictBranches = _currentUser.IsManager ? null : _currentUser.AssignedBranches;
        var all = await _repo.GetByDateRangeAsync(from, to, restrictBranches, ct);

        // Users only see their own transactions.
        if (!_currentUser.IsManager)
            all = all.Where(t => t.CreatedBy == _currentUser.UserId).ToList();

        // A non-Manager viewer only ever sees their own (non-Manager) transactions, so the lookup
        // below is skipped for them, and GetAllAsync() is a Manager-only operation anyway.
        var managerIds = _currentUser.IsManager
            ? (await _userService.GetAllAsync(ct)).Where(u => u.Role == UserRole.Manager).Select(u => u.Id).ToHashSet()
            : new HashSet<string>();

        return new Context { All = all, ManagerIds = managerIds };
    }

    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var ctx = await BuildContextAsync(ct);
        var all = ctx.All;
        var to = DateTime.UtcNow;
        var from = new DateTime(to.Year, to.Month, 1).AddMonths(-11);

        var income = all.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var directManagerExpense = all.Where(ctx.IsDirectManagerExpense).Sum(t => t.Amount);
        var pettyCashIssued = all.Where(ctx.IsApprovedPettyCashFunding).Sum(t => t.Amount);
        var pettyCashExpenses = all.Where(ctx.IsApprovedPettyCashExpense).Sum(t => t.Amount);
        var expense = directManagerExpense + pettyCashIssued;

        var monthly = new List<MonthlySeriesPoint>();
        for (int i = 0; i < 12; i++)
        {
            var m = from.AddMonths(i);
            var monthTx = all.Where(t => t.Date.Year == m.Year && t.Date.Month == m.Month).ToList();
            monthly.Add(new MonthlySeriesPoint
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToString("MMM yyyy"),
                Income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = monthTx.Where(ctx.CountsTowardTotalExpense).Sum(t => t.Amount)
            });
        }

        // Category breakdown reports ALL real spending (Manager direct + User petty-cash-funded),
        // excluding only the internal "Petty Cash" funding category itself, so Managers can trace
        // what each category (and, via the transaction list, each User) actually spent money on.
        var topCategories = all
            .Where(t => t.Category != TransactionCategories.PettyCash)
            .GroupBy(t => t.Category)
            .Select(g => new CategoryTotal { Category = g.Key, Amount = g.Sum(t => t.Amount), Count = g.Count() })
            .OrderByDescending(c => c.Amount)
            .Take(5)
            .ToList();

        var recent = all
            .OrderByDescending(t => t.CreatedDate)
            .Take(10)
            .Select(t => t.ToDto())
            .ToList();

        return new DashboardDto
        {
            TotalIncome = income,
            TotalExpense = expense,
            NetBalance = income - expense,
            TotalPettyCashIssued = pettyCashIssued,
            TotalPettyCashExpenses = pettyCashExpenses,
            TotalPettyCashOutstanding = pettyCashIssued - pettyCashExpenses,
            TransactionCount = all.Count,
            PendingApprovals = all.Count(t => t.ApprovalStatus == ApprovalStatus.Submitted),
            MonthlySeries = monthly,
            TopCategories = topCategories,
            Recent = recent
        };
    }

    public async Task<DashboardBreakdownDto> GetBreakdownAsync(string metric, CancellationToken ct = default)
    {
        var ctx = await BuildContextAsync(ct);
        var all = ctx.All;

        switch (metric)
        {
            case "income":
                return TransactionsBreakdown(all.Where(t => t.Type == TransactionType.Income));

            case "expense":
                return TransactionsBreakdown(all.Where(ctx.CountsTowardTotalExpense));

            case "balance":
                return TransactionsBreakdown(all.Where(t => t.Type == TransactionType.Income || ctx.CountsTowardTotalExpense(t)));

            case "pettyCashIssued":
                return TransactionsBreakdown(all.Where(ctx.IsApprovedPettyCashFunding));

            case "pettyCashExpenses":
                return TransactionsBreakdown(all.Where(ctx.IsApprovedPettyCashExpense));

            case "pettyCashOutstanding":
                var summaries = all
                    .Where(t => ctx.IsApprovedPettyCashFunding(t) || ctx.IsApprovedPettyCashExpense(t))
                    .GroupBy(t => t.CreatedBy)
                    .Select(g =>
                    {
                        var issued = g.Where(ctx.IsApprovedPettyCashFunding).Sum(t => t.Amount);
                        var spent = g.Where(ctx.IsApprovedPettyCashExpense).Sum(t => t.Amount);
                        return new PettyCashUserSummaryDto
                        {
                            UserId = g.Key,
                            DisplayName = g.First().CreatedByName,
                            Issued = issued,
                            Spent = spent,
                            Outstanding = issued - spent
                        };
                    })
                    .OrderByDescending(s => s.Outstanding)
                    .ToList();
                return new DashboardBreakdownDto { UserSummaries = summaries };

            default:
                throw new Common.ValidationAppException(new Dictionary<string, string[]>
                {
                    ["metric"] = new[] { $"Unknown breakdown metric '{metric}'." }
                });
        }
    }

    private static DashboardBreakdownDto TransactionsBreakdown(IEnumerable<Transaction> tx) => new()
    {
        Transactions = tx.OrderByDescending(t => t.Date).Select(t => t.ToDto()).ToList()
    };
}
