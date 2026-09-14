using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace FinanceLedger.API.Auth;

/// <summary>
/// Supabase embeds `app_metadata` (set via the Admin API — see
/// FinanceLedger.Infrastructure.Identity.SupabaseAuthClient) as a JSON object claim
/// on every access token it issues. This maps that into the claim shape the rest of
/// the app already expects: ClaimTypes.Role for [Authorize(Roles=...)] and a
/// repeated "branches" claim for ICurrentUserService.AssignedBranches.
/// </summary>
public class SupabaseClaimsTransformation : IClaimsTransformation
{
    private const string AppMetadataClaimType = "app_metadata";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        // Already transformed (claims transformation can run more than once per request).
        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var appMetadataClaim = identity.FindFirst(AppMetadataClaimType);
        if (appMetadataClaim is null)
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(appMetadataClaim.Value);
            var root = doc.RootElement;

            if (root.TryGetProperty("role", out var roleElement) && roleElement.ValueKind == JsonValueKind.String)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleElement.GetString()!));
            }

            if (root.TryGetProperty("branches", out var branchesElement) && branchesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var branch in branchesElement.EnumerateArray())
                {
                    if (branch.ValueKind == JsonValueKind.String)
                    {
                        identity.AddClaim(new Claim("branches", branch.GetString()!));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed/unexpected app_metadata shape — leave the principal without role/branch claims;
            // downstream [Authorize] checks will simply deny access rather than fail the request.
        }

        return Task.FromResult(principal);
    }
}
