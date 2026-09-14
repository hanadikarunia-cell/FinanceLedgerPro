using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class Invoice : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public InvoiceType Type { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    // Rental invoices
    public string? CarId { get; set; }
    public decimal? MonthlyBill { get; set; }

    // Service Bill invoices
    public string? DriverName { get; set; }
    public decimal? WageDeposit { get; set; }
    public decimal? Fee { get; set; }
    public TaxScheme? TaxScheme { get; set; }

    // Computed at creation time and stored so history/printing stay stable even if tax
    // rates change later.
    public decimal PpnAmount { get; set; }
    public decimal Pph23Amount { get; set; }
    public decimal TotalAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime? PaidDate { get; set; }
    public string? LinkedIncomeTransactionId { get; set; }
    public string? LinkedExpenseTransactionId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
