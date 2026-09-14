using FinanceLedger.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FinanceLedger.API.Services;

/// <summary>Readiness check that confirms the configured Supabase Storage bucket is reachable.</summary>
public class SupabaseStorageHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _options;

    public SupabaseStorageHealthCheck(IHttpClientFactory httpClientFactory, IOptions<SupabaseOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.ServiceRoleKey))
        {
            return HealthCheckResult.Unhealthy("Supabase Storage is not configured.");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{_options.Url.TrimEnd('/')}/storage/v1/bucket/{_options.StorageBucket}");
            request.Headers.Add("apikey", _options.ServiceRoleKey);
            request.Headers.Add("Authorization", $"Bearer {_options.ServiceRoleKey}");

            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Supabase Storage returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Supabase Storage is unreachable.", ex);
        }
    }
}
