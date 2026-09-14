namespace FinanceLedger.Application.DTOs;

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
