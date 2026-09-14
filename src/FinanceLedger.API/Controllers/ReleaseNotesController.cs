using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

/// <summary>The app's "What's New" changelog. Any authenticated user can read it;
/// publishing/editing/deleting entries is Manager-only.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/release-notes")]
[Authorize]
[Produces("application/json")]
public class ReleaseNotesController : ControllerBase
{
    private readonly IReleaseNoteService _service;

    public ReleaseNotesController(IReleaseNoteService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReleaseNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReleaseNoteDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(ReleaseNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReleaseNoteDto>> Create([FromBody] CreateReleaseNoteDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(ReleaseNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReleaseNoteDto>> Update(string id, [FromBody] UpdateReleaseNoteDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
