using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class InvoiceDto
{
    public string Id { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }

    public string? CarId { get; set; }
    public decimal? MonthlyBill { get; set; }

    public string? DriverName { get; set; }
    public decimal? WageDeposit { get; set; }
    public decimal? Fee { get; set; }
    public TaxScheme? TaxScheme { get; set; }

    public decimal PpnAmount { get; set; }
    public decimal Pph23Amount { get; set; }
    public decimal TotalAmount { get; set; }

    public InvoiceStatus Status { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? LinkedIncomeTransactionId { get; set; }
    public string? LinkedExpenseTransactionId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class CreateInvoiceDto
{
    public InvoiceType Type { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }

    // Rental
    public string? CarId { get; set; }

    // Service Bill
    public string? DriverName { get; set; }
    public decimal? WageDeposit { get; set; }
    public decimal? Fee { get; set; }
    public TaxScheme? TaxScheme { get; set; }
}

public class InvoiceQuery
{
    public string? Branch { get; set; }
    public InvoiceType? Type { get; set; }
    public InvoiceStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
