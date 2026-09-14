using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("daily")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDto>> Daily([FromQuery] DateTime? date, CancellationToken ct)
        => Ok(await _reportService.GetDailyAsync(date ?? DateTime.UtcNow.Date, ct));

    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDto>> Monthly([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (year <= 0)
        {
            year = DateTime.UtcNow.Year;
        }

        if (month < 1 || month > 12)
        {
            month = DateTime.UtcNow.Month;
        }

        return Ok(await _reportService.GetMonthlyAsync(year, month, ct));
    }

    [HttpGet("yearly")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDto>> Yearly([FromQuery] int year, CancellationToken ct)
    {
        if (year <= 0)
        {
            year = DateTime.UtcNow.Year;
        }

        return Ok(await _reportService.GetYearlyAsync(year, ct));
    }
}
