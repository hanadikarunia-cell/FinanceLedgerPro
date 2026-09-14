namespace FinanceLedger.Application.DTOs;

public class DashboardDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
    public decimal TotalPettyCashIssued { get; set; }
    public decimal TotalPettyCashExpenses { get; set; }
    public decimal TotalPettyCashOutstanding { get; set; }
    public int TransactionCount { get; set; }
    public int PendingApprovals { get; set; }
    public List<MonthlySeriesPoint> MonthlySeries { get; set; } = new();
    public List<CategoryTotal> TopCategories { get; set; } = new();
    public List<TransactionDto> Recent { get; set; } = new();
}

public class MonthlySeriesPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Income - Expense;
}

public class CategoryTotal
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class DashboardBreakdownDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
    public List<PettyCashUserSummaryDto> UserSummaries { get; set; } = new();
}

public class PettyCashUserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal Issued { get; set; }
    public decimal Spent { get; set; }
    public decimal Outstanding { get; set; }
}
