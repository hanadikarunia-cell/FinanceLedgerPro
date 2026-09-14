using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class AuditRepository : RepositoryBase<AuditLog>, IAuditRepository
{
    public AuditRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<AuditLog>> QueryAsync(
        int page,
        int pageSize,
        string? entity,
        string? userId,
        CancellationToken ct = default)
    {
        IQueryable<AuditLog> query = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        var all = await query.ToListAsync(ct);
        var ordered = all.OrderByDescending(a => a.Timestamp).ToList();

        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? 20 : pageSize;

        var items = ordered
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToList();

        return new PagedResult<AuditLog>(items, ordered.Count, normalizedPage, normalizedSize);
    }
}
