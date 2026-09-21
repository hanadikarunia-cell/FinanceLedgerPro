using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string[] AssignedBranches { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    /// <summary>The user's site; empty for an AppAdmin.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>The site's display name, filled in where it's cheap to look up (sign-in, /auth/me).</summary>
    public string? TenantName { get; set; }
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string[] AssignedBranches { get; set; } = Array.Empty<string>();
    public string? Password { get; set; }
}

public class UpdateUserDto
{
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string[] AssignedBranches { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
}

/// <summary>Manager-initiated password reset for another user — no proof of the old
/// password is required, since Manager authority is itself the check (distinct from
/// the self-service /auth/reset-password flow, which requires the current password).</summary>
public class ResetUserPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}
