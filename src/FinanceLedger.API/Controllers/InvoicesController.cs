using Asp.Versioning;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
[Authorize(Policy = "ManagerOnly")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> Query(
        [FromQuery] string? branch,
        [FromQuery] InvoiceType? type,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new InvoiceQuery
        {
            Branch = branch,
            Type = type,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await _service.QueryAsync(query, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Query), new { version = "1.0" }, created);
    }

    [HttpPost("{id}/mark-paid")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> MarkPaid(string id, CancellationToken ct)
        => Ok(await _service.MarkPaidAsync(id, ct));

    [HttpPost("{id}/void")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> Void(string id, CancellationToken ct)
        => Ok(await _service.VoidAsync(id, ct));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
