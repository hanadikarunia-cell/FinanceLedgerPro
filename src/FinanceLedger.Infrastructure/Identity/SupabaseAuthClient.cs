using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;
using FinanceLedger.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FinanceLedger.Infrastructure.Identity;

/// <summary>
/// Thin REST client over Supabase Auth (GoTrue): user-facing token grants with the
/// anon key, and Admin API calls with the service_role key for provisioning and
/// keeping RBAC claims (role/branches, stored as app_metadata) in sync. app_metadata
/// is embedded by Supabase into every access token it issues, so no separate
/// Postgres Auth Hook is needed to surface it as a claim.
/// </summary>
public class SupabaseAuthClient : IIdentityProviderService
{
    private readonly HttpClient _http;
    private readonly SupabaseOptions _options;

    public SupabaseAuthClient(HttpClient http, IOptions<SupabaseOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/auth/v1/");
    }

    public async Task<AuthGrantResult> PasswordGrantAsync(string email, string password, CancellationToken ct = default)
    {
        using var request = AnonRequest(HttpMethod.Post, "token?grant_type=password");
        request.Content = JsonContent.Create(new { email, password });

        using var response = await _http.SendAsync(request, ct);
        await EnsureAuthSuccessAsync(response, "Invalid credentials.", ct);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Supabase Auth returned an empty token response.");

        return ToGrantResult(payload);
    }

    public async Task<AuthGrantResult> RefreshGrantAsync(string refreshToken, CancellationToken ct = default)
    {
        using var request = AnonRequest(HttpMethod.Post, "token?grant_type=refresh_token");
        request.Content = JsonContent.Create(new { refresh_token = refreshToken });

        using var response = await _http.SendAsync(request, ct);
        await EnsureAuthSuccessAsync(response, "Invalid or expired refresh token.", ct);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Supabase Auth returned an empty token response.");

        return ToGrantResult(payload);
    }

    public async Task RevokeAsync(string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "logout");
        request.Headers.Add("apikey", _options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Best-effort: an already-expired/invalid token shouldn't block client-side logout.
        using var response = await _http.SendAsync(request, ct);
    }

    public async Task<string> AdminCreateUserAsync(
        string email,
        string? password,
        UserRole role,
        IReadOnlyCollection<string> assignedBranches,
        CancellationToken ct = default)
    {
        using var request = AdminRequest(HttpMethod.Post, "admin/users");
        request.Content = JsonContent.Create(new
        {
            email,
            password,
            email_confirm = true,
            app_metadata = new { role = role.ToString(), branches = assignedBranches }
        });

        using var response = await _http.SendAsync(request, ct);
        await EnsureAuthSuccessAsync(response, "Failed to create the user in the identity provider.", ct);

        var payload = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Supabase Auth returned an empty user response.");

        return payload.Id;
    }

    public async Task AdminUpdateUserAsync(
        string identityUserId,
        UserRole? role = null,
        IReadOnlyCollection<string>? assignedBranches = null,
        string? password = null,
        CancellationToken ct = default)
    {
        object? appMetadata = role is null && assignedBranches is null
            ? null
            : new { role = role?.ToString(), branches = assignedBranches };

        using var request = AdminRequest(HttpMethod.Put, $"admin/users/{identityUserId}");
        request.Content = JsonContent.Create(new
        {
            password,
            app_metadata = appMetadata
        });

        using var response = await _http.SendAsync(request, ct);
        await EnsureAuthSuccessAsync(response, "Failed to update the user in the identity provider.", ct);
    }

    private HttpRequestMessage AnonRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("apikey", _options.AnonKey);
        return request;
    }

    private HttpRequestMessage AdminRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("apikey", _options.ServiceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        return request;
    }

    private static async Task EnsureAuthSuccessAsync(HttpResponseMessage response, string failureMessage, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new IdentityProviderException(failureMessage, response.StatusCode, body);
    }

    private static AuthGrantResult ToGrantResult(TokenResponse payload) => new()
    {
        AccessToken = payload.AccessToken,
        RefreshToken = payload.RefreshToken,
        ExpiresAt = DateTime.UtcNow.AddSeconds(payload.ExpiresIn),
        UserId = payload.User.Id
    };

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("user")]
        public AdminUserResponse User { get; set; } = new();
    }

    private sealed class AdminUserResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
