using Asp.Versioning;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

/// <summary>
/// Operational endpoints for a Manager to trigger on-demand background work.
/// On the free-tier deploy, this stands in for a dedicated always-on worker
/// process: a scheduled GitHub Actions workflow calls this on an interval
/// (see .github/workflows/sync-google-sheets.yml) instead of paying for a
/// Render Background Worker just to run the same logic continuously.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "AppAdminOnly")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IConfiguration _configuration;

    public AdminController(ISyncService syncService, IConfiguration configuration)
    {
        _syncService = syncService;
        _configuration = configuration;
    }

    [HttpPost("sync/google-sheets")]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncResultDto>> SyncGoogleSheets(CancellationToken ct)
    {
        // Off unless explicitly enabled: the target spreadsheet is a single global setting, so
        // syncing while several sites exist could copy one site's transactions into another
        // site's sheet. (It also only ever sees the caller's own site, which is none for the
        // Application Admin.) Turn on only once a per-site spreadsheet setting exists.
        if (!_configuration.GetValue<bool>("Sync:Enabled"))
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Google Sheets sync is disabled.");
        }

        var synced = await _syncService.SyncApprovedTransactionsAsync(ct);
        return Ok(new SyncResultDto(synced));
    }
}

public record SyncResultDto(int TransactionsSynced);
