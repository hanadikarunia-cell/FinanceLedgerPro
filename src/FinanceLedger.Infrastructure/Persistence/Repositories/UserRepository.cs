using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToLower();
        return await Set.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.ToListAsync(ct);
    }

    // ---- AnyTenant: the only place user lookups step outside the current site. Login,
    // the user-context middleware and site management have no (or a different) site
    // when they run, so they must see across sites. Keep these few and explicit.

    public async Task<User?> GetByEmailAnyTenantAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToLower();
        return await Set.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public async Task<User?> GetByIdAnyTenantAsync(string id, CancellationToken ct = default)
    {
        return await Set.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IReadOnlyList<User>> GetByTenantAnyTenantAsync(string tenantId, CancellationToken ct = default)
    {
        return await Set.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<int> CountByTenantAnyTenantAsync(string tenantId, CancellationToken ct = default)
    {
        return await Set.IgnoreQueryFilters().CountAsync(u => u.TenantId == tenantId, ct);
    }
}
