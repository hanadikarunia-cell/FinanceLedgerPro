using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public BranchService(IBranchRepository repo, ICurrentUserService currentUser, IAuditService audit)
    {
        _repo = repo;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken ct = default)
    {
        var branches = await _repo.GetAllAsync(ct);
        if (!_currentUser.IsManager)
            branches = branches.Where(b => _currentUser.AssignedBranches.Contains(b.Id)).ToList();
        return branches.Select(b => b.ToDto()).ToList();
    }

    public async Task<BranchDto> CreateAsync(CreateBranchDto dto, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can create branches.");

        var existing = await _repo.GetByCodeAsync(dto.Code, ct);
        if (existing is not null)
            throw new ConflictException($"A branch with code '{dto.Code}' already exists.");

        var branch = new Branch
        {
            Name = dto.Name,
            Code = dto.Code,
            Address = dto.Address,
            IsActive = dto.IsActive
        };

        var created = await _repo.AddAsync(branch, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Branch), created.Id, null, created.ToDto(), ct);
        return created.ToDto();
    }
}
