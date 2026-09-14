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
[Route("api/v{version:apiVersion}/transactions")]
[Authorize]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IUserService _userService;

    public TransactionsController(ITransactionService transactionService, IUserService userService)
    {
        _transactionService = transactionService;
        _userService = userService;
    }

    /// <summary>Minimal user lookup for linking a transaction (e.g. Salaries) to an employee.
    /// Deliberately not behind the ManagerOnly policy on UsersController — any authenticated
    /// user needs this to record a Salaries expense against a colleague.</summary>
    [HttpGet("users-lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUsersLookup(CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(ct);
        return Ok(users.Select(u => new { u.Id, u.DisplayName }));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TransactionDto>>> Query(
        [FromQuery] TransactionType? type,
        [FromQuery] string? category,
        [FromQuery] string? branch,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ApprovalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new TransactionQuery
        {
            Type = type,
            Category = category,
            Branch = branch,
            UserId = userId,
            From = from,
            To = to,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await _transactionService.QueryAsync(query, ct));
    }

    [HttpGet("petty-cash-balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPettyCashBalance(CancellationToken ct)
    {
        var balance = await _transactionService.GetPettyCashBalanceAsync(ct);
        return Ok(new { balance });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetById(string id, CancellationToken ct)
    {
        var result = await _transactionService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        var created = await _transactionService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionDto>> Update(string id, [FromBody] UpdateTransactionDto dto, CancellationToken ct)
        => Ok(await _transactionService.UpdateAsync(id, dto, ct));

    [HttpDelete("{id}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _transactionService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionDto>> Approve(string id, CancellationToken ct)
        => Ok(await _transactionService.ApproveAsync(id, ct));

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionDto>> Reject(string id, [FromBody] RejectTransactionDto dto, CancellationToken ct)
        => Ok(await _transactionService.RejectAsync(id, dto?.Reason, ct));
}
