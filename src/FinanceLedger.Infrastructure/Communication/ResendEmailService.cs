using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FinanceLedger.Infrastructure.Communication;

/// <summary>Approval notification emails via Resend's free-tier HTTP API.</summary>
public class ResendEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly ResendOptions _options;

    public ResendEmailService(HttpClient http, IOptions<ResendOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri("https://api.resend.com/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("emails", new
        {
            from = _options.SenderAddress,
            to = new[] { toEmail },
            subject,
            html = htmlBody
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Resend request failed ({(int)response.StatusCode}): {body}");
        }
    }
}
