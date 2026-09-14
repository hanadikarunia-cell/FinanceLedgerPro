namespace FinanceLedger.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task<(Stream content, string contentType)?> DownloadAsync(string blobName, CancellationToken ct = default);
    Task DeleteAsync(string blobName, CancellationToken ct = default);
}
