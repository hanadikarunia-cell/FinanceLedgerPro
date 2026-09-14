using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

/// <summary>Request body for POST /export/excel|pdf|csv.</summary>
public class ExportRequest
{
    public TransactionType? Type { get; set; }
    public string? Category { get; set; }
    public string? Branch { get; set; }
    public string? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public ApprovalStatus? Status { get; set; }
    public string? Title { get; set; }
    public string? CompanyName { get; set; }
}

public class FileResultDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class FileUploadResultDto
{
    public string AttachmentId { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
}
