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
[Authorize(Policy = "ManagerOnly")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ISyncService _syncService;

    public AdminController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("sync/google-sheets")]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SyncResultDto>> SyncGoogleSheets(CancellationToken ct)
    {
        var synced = await _syncService.SyncApprovedTransactionsAsync(ct);
        return Ok(new SyncResultDto(synced));
    }
}

public record SyncResultDto(int TransactionsSynced);
