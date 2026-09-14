using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class BranchRepository : RepositoryBase<Branch>, IBranchRepository
{
    public BranchRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.ToListAsync(ct);
    }

    public async Task<Branch?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var candidates = await Set.ToListAsync(ct);
        return candidates.FirstOrDefault(b => string.Equals(b.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}
