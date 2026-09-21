using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Entities;

public class User : ITenantEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>The site this user belongs to. Empty for an AppAdmin, who belongs to no site.</summary>
    public string TenantId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string[] AssignedBranches { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unused since the move to Supabase Auth, which owns credential storage.
    /// Kept nullable for backward-compatible schema/data.
    /// </summary>
    public string? PasswordHash { get; set; }
}
