using System.Security.Claims;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.API.Services;

/// <summary>Claim names the user-context middleware puts on the effective principal.</summary>
public static class TenantClaims
{
    public const string TenantId = "tenant_id";
    public const string ActingAdminId = "act_as_by";
    public const string ActingAdminName = "act_as_by_name";
    public const string ActingCanWrite = "act_as_write";
}

public class CurrentUserService : ICurrentUserService, ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        FirstClaim(ClaimTypes.NameIdentifier, "sub", "oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");

    public string? UserName =>
        FirstClaim(ClaimTypes.Name, "name", "preferred_username");

    public string? Email =>
        FirstClaim(ClaimTypes.Email, "email", "emails", "preferred_username");

    public UserRole? Role
    {
        get
        {
            var raw = FirstClaim(ClaimTypes.Role, "role", "roles");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return Enum.TryParse<UserRole>(raw, ignoreCase: true, out var role) ? role : null;
        }
    }

    public IReadOnlyCollection<string> AssignedBranches =>
        Principal?.FindAll("branches").Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool IsManager => Role == UserRole.Manager;

    public bool IsAppAdmin => Role == UserRole.AppAdmin;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    // Only the user-context middleware sets this claim (from the database, never from
    // the token), so a client cannot choose its own site.
    public string? TenantId => FirstClaim(TenantClaims.TenantId);

    public bool IsActingAs => !string.IsNullOrWhiteSpace(ActingAdminId);

    public string? ActingAdminId => FirstClaim(TenantClaims.ActingAdminId);

    public string? ActingAdminName => FirstClaim(TenantClaims.ActingAdminName);

    public bool ActingCanWrite => FirstClaim(TenantClaims.ActingCanWrite) == "true";

    private string? FirstClaim(params string[] claimTypes)
    {
        var principal = Principal;
        if (principal is null)
        {
            return null;
        }

        foreach (var type in claimTypes)
        {
            var value = principal.FindFirstValue(type);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
