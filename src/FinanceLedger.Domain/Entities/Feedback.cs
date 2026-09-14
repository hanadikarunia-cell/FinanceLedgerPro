using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class Feedback : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;

    /// <summary>URL of an image pasted/attached to the feedback, if any. Uploaded via
    /// the existing attachment pipeline (Supabase Storage), same as transaction receipts.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Null until a Manager triages it.</summary>
    public FeedbackSeverity? Severity { get; set; }

    public string SubmittedBy { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    /// <summary>App version the submitter was using, so triage knows which build a
    /// report applies to.</summary>
    public string AppVersion { get; set; } = string.Empty;
}
