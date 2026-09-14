using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/branches")]
[Authorize]
[Produces("application/json")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetAll(CancellationToken ct)
        => Ok(await _branchService.GetAllAsync(ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BranchDto>> Create([FromBody] CreateBranchDto dto, CancellationToken ct)
    {
        var created = await _branchService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, created);
    }
}
