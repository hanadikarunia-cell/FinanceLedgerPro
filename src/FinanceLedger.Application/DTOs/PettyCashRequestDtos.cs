using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class PettyCashRequestDto
{
    public string Id { get; set; } = string.Empty;
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

public class CreatePettyCashRequestDto
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public class PettyCashRequestQuery
{
    public string? Branch { get; set; }
    public ApprovalStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
