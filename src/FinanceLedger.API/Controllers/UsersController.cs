using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = "ManagerOnly")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
        => Ok(await _userService.GetAllAsync(ct));

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var created = await _userService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        => Ok(await _userService.UpdateAsync(id, dto, ct));

    /// <summary>Manager resets another user's password directly — no proof of the old
    /// password is required. Distinct from the self-service POST /auth/reset-password,
    /// which requires the caller's own current password.</summary>
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetUserPasswordDto dto, CancellationToken ct)
    {
        await _userService.ResetPasswordAsync(id, dto.NewPassword, ct);
        return NoContent();
    }
}
