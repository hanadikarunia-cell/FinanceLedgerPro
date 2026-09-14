using Asp.Versioning;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/png",
            "image/jpeg",
            "image/jpg"
        };

    private readonly IBlobStorageService _blobStorage;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ICurrentUserService _currentUser;

    public FilesController(
        IBlobStorageService blobStorage,
        IAttachmentRepository attachmentRepository,
        ICurrentUserService currentUser)
    {
        _blobStorage = blobStorage;
        _attachmentRepository = attachmentRepository;
        _currentUser = currentUser;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(FileUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResultDto>> Upload(
        IFormFile file,
        [FromForm] string? transactionId,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "A non-empty file is required." }
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "File exceeds the maximum allowed size of 10 MB." }
            });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "Only PDF, PNG, JPG, and JPEG files are permitted." }
            });
        }

        await using var stream = file.OpenReadStream();
        var blobUrl = await _blobStorage.UploadAsync(file.FileName, file.ContentType, stream, ct);

        var attachment = new Attachment
        {
            TransactionId = transactionId ?? string.Empty,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            BlobUrl = blobUrl,
            UploadedBy = _currentUser.UserId ?? string.Empty,
            UploadedDate = DateTime.UtcNow
        };

        var saved = await _attachmentRepository.AddAsync(attachment, ct);

        return Ok(new FileUploadResultDto
        {
            AttachmentId = saved.Id,
            BlobUrl = saved.BlobUrl
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id, ct);
        if (attachment is null)
        {
            throw new NotFoundException("Attachment", id);
        }

        var blobName = ResolveBlobName(attachment.BlobUrl);
        var download = await _blobStorage.DownloadAsync(blobName, ct);
        if (download is null)
        {
            throw new NotFoundException("Attachment content", id);
        }

        var contentType = string.IsNullOrWhiteSpace(download.Value.contentType)
            ? attachment.ContentType
            : download.Value.contentType;

        return File(download.Value.content, contentType, attachment.FileName);
    }

    private static string ResolveBlobName(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            return blobUrl;
        }

        return Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri)
            ? uri.Segments[^1]
            : blobUrl;
    }
}
