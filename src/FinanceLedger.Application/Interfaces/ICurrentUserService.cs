using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Interfaces;

/// <summary>
/// The effective identity of the caller. While an admin is "acting as" another user,
/// every member here describes the user being acted as (so all existing role/branch
/// checks apply to them), and the Acting* members describe the real admin behind it.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    UserRole? Role { get; }
    IReadOnlyCollection<string> AssignedBranches { get; }

    /// <summary>True only for a Site Admin (Manager) — not for an AppAdmin.</summary>
    bool IsManager { get; }
    bool IsAppAdmin { get; }
    bool IsAuthenticated { get; }

    /// <summary>The site the caller is scoped to; null for an AppAdmin outside any site.</summary>
    string? TenantId { get; }

    bool IsActingAs { get; }
    string? ActingAdminId { get; }
    string? ActingAdminName { get; }
    bool ActingCanWrite { get; }
}
