using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

/// <summary>An admin-published "What's New" entry — distinct from user-submitted
/// Feedback: this is the app's own changelog, authored by a Manager.</summary>
public class ReleaseNote : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ReleaseType Type { get; set; }
    public string[] Notes { get; set; } = Array.Empty<string>();
    public string PublishedBy { get; set; } = string.Empty;
    public string PublishedByName { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
}
