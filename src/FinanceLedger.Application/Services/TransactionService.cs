using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Constants;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public TransactionService(ITransactionRepository repo, ICurrentUserService currentUser, IAuditService audit)
    {
        _repo = repo;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<TransactionDto>> QueryAsync(TransactionQuery query, CancellationToken ct = default)
    {
        // A regular User is restricted to their own transactions within assigned branches.
        IReadOnlyCollection<string>? restrictBranches = null;
        string? restrictUserId = null;
        if (!_currentUser.IsManager)
        {
            restrictBranches = _currentUser.AssignedBranches;
            restrictUserId = _currentUser.UserId;
        }

        var page = await _repo.QueryAsync(query, restrictBranches, restrictUserId, ct);
        return new PagedResult<TransactionDto>(
            page.Items.Select(x => x.ToDto()).ToList(),
            page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<TransactionDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;
        EnsureCanView(entity);
        return entity.ToDto();
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionDto dto, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager && !_currentUser.AssignedBranches.Contains(dto.Branch))
            throw new ForbiddenException("You can only create transactions in your assigned branches.");

        if (!_currentUser.IsManager
            && dto.Type == TransactionType.Expense
            && !string.Equals(dto.Category, TransactionCategories.PettyCash, StringComparison.OrdinalIgnoreCase))
        {
            var balance = await GetPettyCashBalanceAsync(_currentUser.UserId!, ct);
            if (dto.Amount > balance)
                throw new ConflictException(
                    $"Insufficient petty cash balance. Available: {balance:N0}, requested: {dto.Amount:N0}.");
        }

        var entity = new Transaction
        {
            Type = dto.Type,
            Category = dto.Category,
            Description = dto.Description,
            Amount = dto.Amount,
            Date = dto.Date,
            Branch = dto.Branch,
            CreatedBy = _currentUser.UserId ?? "unknown",
            CreatedByName = _currentUser.UserName ?? "unknown",
            CreatedDate = DateTime.UtcNow,
            ApprovalStatus = TransactionCategories.RequiresApproval.Contains(dto.Category)
                ? ApprovalStatus.Submitted
                : ApprovalStatus.Approved,
            AttachmentIds = dto.AttachmentIds,
            RelatedUserId = dto.RelatedUserId,
            CarId = dto.CarId
        };

        var created = await _repo.AddAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Transaction), created.Id, null, created, ct);
        return created.ToDto();
    }

    public async Task<TransactionDto> UpdateAsync(string id, UpdateTransactionDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Transaction), id);

        EnsureCanView(entity);

        if (!_currentUser.IsManager)
        {
            if (entity.CreatedBy != _currentUser.UserId)
                throw new ForbiddenException("You can only edit your own transactions.");
            if (entity.ApprovalStatus == ApprovalStatus.Approved)
                throw new ForbiddenException("Approved transactions cannot be edited.");

            if (dto.Type == TransactionType.Expense
                && !string.Equals(dto.Category, TransactionCategories.PettyCash, StringComparison.OrdinalIgnoreCase))
            {
                var balance = await GetPettyCashBalanceAsync(_currentUser.UserId!, ct);
                var availableForEdit = balance + entity.Amount;
                if (dto.Amount > availableForEdit)
                    throw new ConflictException(
                        $"Insufficient petty cash balance. Available: {availableForEdit:N0}, requested: {dto.Amount:N0}.");
            }
        }

        var old = Clone(entity);
        entity.Type = dto.Type;
        entity.Category = dto.Category;
        entity.Description = dto.Description;
        entity.Amount = dto.Amount;
        entity.Date = dto.Date;
        entity.Branch = dto.Branch;
        entity.AttachmentIds = dto.AttachmentIds;
        entity.RelatedUserId = dto.RelatedUserId;
        entity.CarId = dto.CarId;

        var updated = await _repo.UpdateAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Update, nameof(Transaction), updated.Id, old, updated, ct);
        return updated.ToDto();
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can delete transactions.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Transaction), id);

        await _repo.DeleteAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Delete, nameof(Transaction), id, entity, null, ct);
    }

    public async Task<TransactionDto> ApproveAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can approve transactions.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Transaction), id);

        var old = Clone(entity);
        entity.ApprovalStatus = ApprovalStatus.Approved;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedDate = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Approve, nameof(Transaction), id, old, updated, ct);
        return updated.ToDto();
    }

    public async Task<TransactionDto> RejectAsync(string id, string? reason, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can reject transactions.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Transaction), id);

        var old = Clone(entity);
        entity.ApprovalStatus = ApprovalStatus.Rejected;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
            entity.Description = $"{entity.Description}\n[Rejected: {reason}]";

        var updated = await _repo.UpdateAsync(entity, ct);
        await _audit.LogAsync(AuditAction.Reject, nameof(Transaction), id, old, updated, ct);
        return updated.ToDto();
    }

    public async Task<decimal> GetPettyCashBalanceAsync(CancellationToken ct = default)
    {
        if (_currentUser.IsManager)
            return 0m;
        return await GetPettyCashBalanceAsync(_currentUser.UserId!, ct);
    }

    private async Task<decimal> GetPettyCashBalanceAsync(string userId, CancellationToken ct)
    {
        var issued = (await _repo.FindAsync(
            t => t.CreatedBy == userId
                && t.Type == TransactionType.Expense
                && t.Category == TransactionCategories.PettyCash,
            ct)).Sum(t => t.Amount);

        var spent = (await _repo.FindAsync(
            t => t.CreatedBy == userId
                && t.Type == TransactionType.Expense
                && t.Category != TransactionCategories.PettyCash,
            ct)).Sum(t => t.Amount);

        return issued - spent;
    }

    private void EnsureCanView(Transaction entity)
    {
        if (_currentUser.IsManager) return;
        var inBranch = _currentUser.AssignedBranches.Contains(entity.Branch);
        var isOwn = entity.CreatedBy == _currentUser.UserId;
        if (!inBranch || !isOwn)
            throw new ForbiddenException("You do not have access to this transaction.");
    }

    private static Transaction Clone(Transaction t) => new()
    {
        Id = t.Id,
        Type = t.Type,
        Category = t.Category,
        Description = t.Description,
        Amount = t.Amount,
        Date = t.Date,
        Branch = t.Branch,
        CreatedBy = t.CreatedBy,
        CreatedByName = t.CreatedByName,
        CreatedDate = t.CreatedDate,
        ApprovalStatus = t.ApprovalStatus,
        ApprovedBy = t.ApprovedBy,
        ApprovedDate = t.ApprovedDate,
        AttachmentIds = t.AttachmentIds,
        RelatedUserId = t.RelatedUserId,
        CarId = t.CarId
    };
}
