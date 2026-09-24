using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Sites are looked up before any site context exists (login, the user-context middleware) and
/// managed by the Application Admin, who belongs to no site - so every method here runs in a
/// bypass scope. Row-level security on `tenants` only lets a normal site session read its own
/// row and never write, which is why nothing outside this class touches the table.
/// </summary>
public class TenantRepository : RepositoryBase<Tenant>, ITenantRepository
{
    public TenantRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Tenant>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await Set.OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await Set.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        var normalized = code.ToLower();
        return await Set.FirstOrDefaultAsync(t => t.Code.ToLower() == normalized, ct);
    }

    public override async Task<Tenant> AddAsync(Tenant entity, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await base.AddAsync(entity, ct);
    }

    public override async Task<Tenant> UpdateAsync(Tenant entity, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await base.UpdateAsync(entity, ct);
    }
}
