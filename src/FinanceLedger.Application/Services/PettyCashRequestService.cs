using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Constants;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class PettyCashRequestService : IPettyCashRequestService
{
    private readonly IPettyCashRequestRepository _repo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public PettyCashRequestService(
        IPettyCashRequestRepository repo,
        ITransactionRepository transactionRepo,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _repo = repo;
        _transactionRepo = transactionRepo;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<PettyCashRequestDto>> QueryAsync(PettyCashRequestQuery query, CancellationToken ct = default)
    {
        IReadOnlyCollection<string>? restrictBranches = null;
        string? restrictUserId = null;
        if (!_currentUser.IsManager)
        {
            restrictBranches = _currentUser.AssignedBranches;
            restrictUserId = _currentUser.UserId;
        }

        var page = await _repo.QueryAsync(query, restrictBranches, restrictUserId, ct);
        return new PagedResult<PettyCashRequestDto>(
            page.Items.Select(x => x.ToDto()).ToList(),
            page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<PettyCashRequestDto> CreateAsync(CreatePettyCashRequestDto dto, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager && !_currentUser.AssignedBranches.Contains(dto.Branch))
            throw new ForbiddenException("You can only request petty cash for your assigned branches.");

        var entity = new PettyCashRequest
        {
            Amount = dto.Amount,
            Reason = dto.Reason,
            Branch = dto.Branch,
            RequestedBy = _currentUser.UserId ?? "unknown",
            RequestedByName = _currentUser.UserName ?? "unknown",
            RequestedDate = DateTime.UtcNow,
            Status = ApprovalStatus.Submitted
        };

        var created = await _repo.AddAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(PettyCashRequest), created.Id, null, created, ct);
        return created.ToDto();
    }

    public async Task<PettyCashRequestDto> ApproveAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can approve petty cash requests.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PettyCashRequest), id);

        if (entity.Status != ApprovalStatus.Submitted)
            throw new ConflictException("Only pending requests can be approved.");

        var old = Clone(entity);

        // Built directly against the repository (not ITransactionService.CreateAsync) so the
        // resulting ledger entry is attributed to the original requester/custodian, not the
        // approving Manager — this is what the per-user petty cash balance is computed from.
        var transactionEntity = new Transaction
        {
            Type = TransactionType.Expense,
            Category = TransactionCategories.PettyCash,
            Description = $"Petty cash issued: {entity.Reason}",
            Amount = entity.Amount,
            Date = DateTime.UtcNow,
            Branch = entity.Branch,
            CreatedBy = entity.RequestedBy,
            CreatedByName = entity.RequestedByName,
            CreatedDate = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.Approved,
            ApprovedBy = _currentUser.UserId,
            ApprovedDate = DateTime.UtcNow
        };

        var transaction = await _transactionRepo.AddAsync(transactionEntity, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Transaction), transaction.Id, null, transaction, ct);

        entity.Status = ApprovalStatus.Approved;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedDate = DateTime.UtcNow;
        entity.LinkedTransactionId = transaction.Id;

        var updated = await _repo.UpdateAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Approve, nameof(PettyCashRequest), id, old, updated, ct);
        return updated.ToDto();
    }

    public async Task<PettyCashRequestDto> RejectAsync(string id, string? reason, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can reject petty cash requests.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PettyCashRequest), id);

        if (entity.Status != ApprovalStatus.Submitted)
            throw new ConflictException("Only pending requests can be rejected.");

        var old = Clone(entity);
        entity.Status = ApprovalStatus.Rejected;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
            entity.Reason = $"{entity.Reason}\n[Rejected: {reason}]";

        var updated = await _repo.UpdateAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Reject, nameof(PettyCashRequest), id, old, updated, ct);
        return updated.ToDto();
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can delete petty cash requests.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PettyCashRequest), id);

        // An Approved request already has a linked ledger transaction (the funding entry) —
        // deleting the request without touching that transaction would leave the books with an
        // untraceable disbursement, so it must be handled as a transaction correction instead.
        if (entity.Status == ApprovalStatus.Approved)
            throw new ForbiddenException("Approved petty cash requests cannot be deleted; delete the linked transaction instead.");

        await _repo.DeleteAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Delete, nameof(PettyCashRequest), id, entity, null, ct);
    }

    private static PettyCashRequest Clone(PettyCashRequest p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Reason = p.Reason,
        Branch = p.Branch,
        RequestedBy = p.RequestedBy,
        RequestedByName = p.RequestedByName,
        RequestedDate = p.RequestedDate,
        Status = p.Status,
        ApprovedBy = p.ApprovedBy,
        ApprovedDate = p.ApprovedDate,
        LinkedTransactionId = p.LinkedTransactionId
    };
}
