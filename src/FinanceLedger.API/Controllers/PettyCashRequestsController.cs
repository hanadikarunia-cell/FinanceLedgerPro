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
[Route("api/v{version:apiVersion}/petty-cash-requests")]
[Authorize]
[Produces("application/json")]
public class PettyCashRequestsController : ControllerBase
{
    private readonly IPettyCashRequestService _service;

    public PettyCashRequestsController(IPettyCashRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PettyCashRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PettyCashRequestDto>>> Query(
        [FromQuery] string? branch,
        [FromQuery] ApprovalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new PettyCashRequestQuery
        {
            Branch = branch,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await _service.QueryAsync(query, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PettyCashRequestDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PettyCashRequestDto>> Create([FromBody] CreatePettyCashRequestDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Query), new { version = "1.0" }, created);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(PettyCashRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PettyCashRequestDto>> Approve(string id, CancellationToken ct)
        => Ok(await _service.ApproveAsync(id, ct));

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(PettyCashRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PettyCashRequestDto>> Reject(string id, [FromBody] RejectTransactionDto dto, CancellationToken ct)
        => Ok(await _service.RejectAsync(id, dto?.Reason, ct));

    [HttpDelete("{id}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
