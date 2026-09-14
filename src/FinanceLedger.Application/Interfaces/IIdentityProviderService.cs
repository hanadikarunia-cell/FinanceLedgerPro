using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Interfaces;

/// <summary>
/// Abstracts the external identity provider (Supabase Auth) that owns credential
/// verification and access/refresh token issuance. The returned user id is the
/// identity provider's user id, which is also used as the local <c>users.id</c>
/// primary key so the two stay joined without a separate mapping table.
/// </summary>
public interface IIdentityProviderService
{
    Task<AuthGrantResult> PasswordGrantAsync(string email, string password, CancellationToken ct = default);

    Task<AuthGrantResult> RefreshGrantAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes the given access token's session. Best-effort — failures are not fatal to logout.</summary>
    Task RevokeAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Creates a new identity in the provider with the given role/branches stored as
    /// app metadata (embedded into every access token the provider issues for this user).
    /// Returns the provider's user id.
    /// </summary>
    Task<string> AdminCreateUserAsync(
        string email,
        string? password,
        UserRole role,
        IReadOnlyCollection<string> assignedBranches,
        CancellationToken ct = default);

    /// <summary>Updates app metadata (role/branches) and/or the password for an existing identity.</summary>
    Task AdminUpdateUserAsync(
        string identityUserId,
        UserRole? role = null,
        IReadOnlyCollection<string>? assignedBranches = null,
        string? password = null,
        CancellationToken ct = default);
}

public class AuthGrantResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
}
