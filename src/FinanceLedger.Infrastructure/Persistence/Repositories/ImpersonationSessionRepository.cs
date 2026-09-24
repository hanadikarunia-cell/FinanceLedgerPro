using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Every operation runs in a bypass scope: an Application Admin starts a session for a person
/// in any site, so a site-scoped connection could never read or write these rows. The table is
/// only granted to that path; nothing user-facing queries it directly.
/// </summary>
public class ImpersonationSessionRepository : RepositoryBase<ImpersonationSession>, IImpersonationSessionRepository
{
    public ImpersonationSessionRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<ImpersonationSession?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await Set.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<ImpersonationSession>> GetActiveForAdminAsync(
        string adminUserId, DateTime nowUtc, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await Set
            .Where(s => s.AdminUserId == adminUserId && s.EndedAt == null && s.ExpiresAt > nowUtc)
            .ToListAsync(ct);
    }

    public override async Task<ImpersonationSession> AddAsync(ImpersonationSession entity, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await base.AddAsync(entity, ct);
    }

    public override async Task<ImpersonationSession> UpdateAsync(ImpersonationSession entity, CancellationToken ct = default)
    {
        using var _ = TenantBypassContext.Begin();
        return await base.UpdateAsync(entity, ct);
    }
}
