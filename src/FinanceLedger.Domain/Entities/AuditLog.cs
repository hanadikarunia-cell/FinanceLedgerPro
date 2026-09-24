using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class AuditLog : ITenantEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Empty for application-level actions that happen outside any site.</summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>The effective user: while an admin uses View as, this is the person being viewed.</summary>
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>The real, authenticated user who performed the action. Equals UserId unless impersonating.</summary>
    public string? ActorUserId { get; set; }

    /// <summary>Set only while impersonating: the person the actor was viewing as.</summary>
    public string? ActingAsUserId { get; set; }
    public string? ImpersonationSessionId { get; set; }

    /// <summary>Free-form JSON with anything else worth keeping (IP, reason, ...).</summary>
    public string? Metadata { get; set; }
    public AuditAction Action { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
