using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

/// <summary>Client sites (tenants). Application Admin only.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sites")]
[Authorize(Policy = "AppAdminOnly")]
[Produces("application/json")]
public class SitesController : ControllerBase
{
    private readonly ITenantService _service;

    public SitesController(ITenantService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantDto>> Update(string id, [FromBody] UpdateTenantDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    /// <summary>The people in a site, so the Application Admin can pick someone to "View as".</summary>
    [HttpGet("{id}/users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(string id, CancellationToken ct)
        => Ok(await _service.GetUsersAsync(id, ct));
}
