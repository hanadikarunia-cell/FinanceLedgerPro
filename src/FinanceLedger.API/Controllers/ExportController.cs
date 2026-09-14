using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IExcelExportService _excelExport;
    private readonly IPdfExportService _pdfExport;
    private readonly ICsvExportService _csvExport;

    public ExportController(
        ITransactionRepository transactionRepository,
        ICurrentUserService currentUser,
        IExcelExportService excelExport,
        IPdfExportService pdfExport,
        ICsvExportService csvExport)
    {
        _transactionRepository = transactionRepository;
        _currentUser = currentUser;
        _excelExport = excelExport;
        _pdfExport = pdfExport;
        _csvExport = csvExport;
    }

    [HttpPost("excel")]
    public async Task<IActionResult> Excel([FromBody] ExportRequest request, CancellationToken ct)
        => await ExportAsync(request, _excelExport.Export, ct);

    [HttpPost("pdf")]
    public async Task<IActionResult> Pdf([FromBody] ExportRequest request, CancellationToken ct)
        => await ExportAsync(request, _pdfExport.Export, ct);

    [HttpPost("csv")]
    public async Task<IActionResult> Csv([FromBody] ExportRequest request, CancellationToken ct)
        => await ExportAsync(request, _csvExport.Export, ct);

    private async Task<IActionResult> ExportAsync(
        ExportRequest request,
        Func<IReadOnlyList<Transaction>, ExportRequest, FileResultDto> exporter,
        CancellationToken ct)
    {
        request ??= new ExportRequest();

        var to = request.To ?? DateTime.UtcNow;
        var from = request.From ?? to.AddYears(-1);

        // Non-managers are limited to their assigned branches at the data layer.
        IReadOnlyCollection<string>? restrictBranches =
            _currentUser.IsManager ? null : _currentUser.AssignedBranches;

        var transactions = await _transactionRepository.GetByDateRangeAsync(from, to, restrictBranches, ct);

        var filtered = transactions.Where(t =>
                (!request.Type.HasValue || t.Type == request.Type.Value) &&
                (string.IsNullOrWhiteSpace(request.Category) || string.Equals(t.Category, request.Category, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(request.Branch) || string.Equals(t.Branch, request.Branch, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(request.UserId) || string.Equals(t.CreatedBy, request.UserId, StringComparison.OrdinalIgnoreCase)) &&
                (!request.Status.HasValue || t.ApprovalStatus == request.Status.Value))
            .OrderByDescending(t => t.Date)
            .ToList();

        var result = exporter(filtered, request);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
