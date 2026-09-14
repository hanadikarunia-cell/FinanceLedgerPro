using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class PettyCashRequest : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? LinkedTransactionId { get; set; }
}
