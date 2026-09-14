using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
        => Ok(await _dashboardService.GetAsync(ct));

    [HttpGet("breakdown")]
    [ProducesResponseType(typeof(DashboardBreakdownDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardBreakdownDto>> GetBreakdown([FromQuery] string metric, CancellationToken ct)
        => Ok(await _dashboardService.GetBreakdownAsync(metric, ct));
}
