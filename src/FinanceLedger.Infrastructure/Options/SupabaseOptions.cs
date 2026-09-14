namespace FinanceLedger.Infrastructure.Options;

/// <summary>
/// Configuration for a single Supabase project, covering the three services this
/// app consumes from it: Postgres (via <c>ConnectionStrings:Postgres</c>, kept
/// separate since EF Core reads connection strings from that standard section),
/// Auth, and Storage.
/// </summary>
public class SupabaseOptions
{
    /// <summary>Project URL, e.g. https://xyzcompany.supabase.co</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Public "anon" API key — used for user-facing Auth calls (login/refresh).</summary>
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>Secret "service_role" key — used for Admin API calls and Storage access. Server-only.</summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    /// <summary>Storage bucket used for transaction attachments.</summary>
    public string StorageBucket { get; set; } = "attachments";

    /// <summary>
    /// The JWKS (JSON Web Key Set) endpoint Supabase Auth publishes for its current
    /// asymmetric (ES256) signing key(s) — projects created since Supabase moved off
    /// the legacy shared HS256 JWT secret. The API validates every access token
    /// against these public keys instead of a static secret.
    /// </summary>
    public string JwksUrl => $"{Url.TrimEnd('/')}/auth/v1/.well-known/jwks.json";
}
