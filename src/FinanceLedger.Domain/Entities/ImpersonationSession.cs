using FinanceLedger.Domain.Common;

namespace FinanceLedger.Domain.Entities;

/// <summary>
/// A server-side record of an admin using "View as": who, as whom, in which site, when, and
/// until when. The client only ever holds this session's id; the target is looked up here,
/// never taken from the request. Every session is read-only. Deliberately not a tenant
/// entity: it is only reachable through the bypass-scoped repository.
/// </summary>
public class ImpersonationSession : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>The real, authenticated admin.</summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>The person being viewed.</summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>The target's site.</summary>
    public string TargetTenantId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Null while the session is running.</summary>
    public DateTime? EndedAt { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool IsActive(DateTime nowUtc) => EndedAt is null && ExpiresAt > nowUtc;
}
