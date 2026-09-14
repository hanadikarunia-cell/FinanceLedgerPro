using System.Net.Http.Headers;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FinanceLedger.Infrastructure.Storage;

/// <summary>
/// Attachment storage backed by the Supabase Storage REST API. The bucket is
/// private (no public access, mirroring the previous Blob Storage container), so
/// every read/write goes through the service_role key on the server — clients
/// never talk to Supabase Storage directly.
/// </summary>
public class SupabaseStorageService : IBlobStorageService
{
    private readonly HttpClient _http;
    private readonly SupabaseOptions _options;

    public SupabaseStorageService(HttpClient http, IOptions<SupabaseOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/storage/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        _http.DefaultRequestHeaders.Add("apikey", _options.ServiceRoleKey);
    }

    public async Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var objectKey = $"{Guid.NewGuid():N}{extension}";

        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await _http.PostAsync($"object/{_options.StorageBucket}/{objectKey}", body, ct);
        await EnsureSuccessAsync(response, ct);

        return objectKey;
    }

    public async Task<(Stream content, string contentType)?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"object/{_options.StorageBucket}/{blobName}", HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (new MemoryStream(bytes), contentType);
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"object/{_options.StorageBucket}/{blobName}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"Supabase Storage request failed ({(int)response.StatusCode}): {body}");
    }
}
