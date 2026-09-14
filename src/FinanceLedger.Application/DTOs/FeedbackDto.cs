using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class FeedbackDto
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public FeedbackSeverity? Severity { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; }
    public string AppVersion { get; set; } = string.Empty;
}

public class CreateFeedbackDto
{
    public string Message { get; set; } = string.Empty;

    /// <summary>URL of an already-uploaded image (via POST /files/upload), if any —
    /// the client uploads the pasted image first, then submits its URL here.</summary>
    public string? ImageUrl { get; set; }

    public string AppVersion { get; set; } = string.Empty;
}

public class SetFeedbackSeverityDto
{
    public FeedbackSeverity Severity { get; set; }
}

public class FeedbackQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
