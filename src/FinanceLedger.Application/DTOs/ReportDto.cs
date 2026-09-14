namespace FinanceLedger.Application.DTOs;

/// <summary>
/// Optional refinements on top of a report's period (daily/monthly/yearly). When
/// From/To are given they override the period-computed date range (so the Web
/// Reports page's date pickers can narrow a period instead of being silently
/// ignored); Category/Branch/UserId further filter the transaction set.
/// </summary>
public class ReportQueryOptions
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Category { get; set; }
    public string? Branch { get; set; }
    public string? UserId { get; set; }
}

public class ReportDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
    public decimal TotalPettyCashIssued { get; set; }
    public decimal TotalPettyCashExpenses { get; set; }
    public decimal TotalPettyCashOutstanding { get; set; }
    public int TransactionCount { get; set; }
    public List<CategoryTotal> IncomeByCategory { get; set; } = new();
    public List<CategoryTotal> ExpenseByCategory { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
}
