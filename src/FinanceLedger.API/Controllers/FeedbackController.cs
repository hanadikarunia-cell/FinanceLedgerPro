using Asp.Versioning;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feedback")]
[Authorize]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _service;

    public FeedbackController(IFeedbackService service)
    {
        _service = service;
    }

    /// <summary>The Application Admin sees all feedback; everyone else sees only their own submissions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FeedbackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FeedbackDto>>> Query(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new FeedbackQuery { Page = page, PageSize = pageSize };
        return Ok(await _service.QueryAsync(query, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FeedbackDto>> Create([FromBody] CreateFeedbackDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Query), new { version = "1.0" }, created);
    }

    [HttpPut("{id}/severity")]
    [Authorize(Policy = "AppAdminOnly")]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeedbackDto>> SetSeverity(string id, [FromBody] SetFeedbackSeverityDto dto, CancellationToken ct)
        => Ok(await _service.SetSeverityAsync(id, dto.Severity, ct));
}
