using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string[] AttachmentIds { get; set; } = Array.Empty<string>();
    public string? RelatedUserId { get; set; }
    public string? CarId { get; set; }
}

public class CreateTransactionDto
{
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string[] AttachmentIds { get; set; } = Array.Empty<string>();
    public string? RelatedUserId { get; set; }
    public string? CarId { get; set; }
}

public class UpdateTransactionDto
{
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string[] AttachmentIds { get; set; } = Array.Empty<string>();
    public string? RelatedUserId { get; set; }
    public string? CarId { get; set; }
}

/// <summary>Query/filter object for GET /transactions.</summary>
public class TransactionQuery
{
    public TransactionType? Type { get; set; }
    public string? Category { get; set; }
    public string? Branch { get; set; }
    public string? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public ApprovalStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RejectTransactionDto
{
    public string? Reason { get; set; }
}
