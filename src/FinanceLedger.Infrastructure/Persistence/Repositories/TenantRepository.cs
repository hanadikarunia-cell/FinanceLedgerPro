using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class TenantRepository : RepositoryBase<Tenant>, ITenantRepository
{
    public TenantRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Tenant>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        return await Set.OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.ToLower();
        return await Set.FirstOrDefaultAsync(t => t.Code.ToLower() == normalized, ct);
    }
}
