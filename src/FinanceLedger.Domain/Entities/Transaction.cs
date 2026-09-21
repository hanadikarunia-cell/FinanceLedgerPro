using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class Transaction : ITenantEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    /// <summary>Branch identifier.</summary>
    public string Branch { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public string[] AttachmentIds { get; set; } = Array.Empty<string>();

    /// <summary>Employee this transaction relates to. Used for Expense/Salaries entries.</summary>
    public string? RelatedUserId { get; set; }

    /// <summary>Car this transaction relates to. Used for Expense/Service and Expense/Car Debt entries.</summary>
    public string? CarId { get; set; }
}
