using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class PettyCashRequestRepository : RepositoryBase<PettyCashRequest>, IPettyCashRequestRepository
{
    public PettyCashRequestRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<PettyCashRequest>> QueryAsync(
        PettyCashRequestQuery query,
        IReadOnlyCollection<string>? restrictBranches,
        string? restrictUserId,
        CancellationToken ct = default)
    {
        IQueryable<PettyCashRequest> serverQuery = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Branch))
            serverQuery = serverQuery.Where(p => p.Branch == query.Branch);

        if (query.Status.HasValue)
            serverQuery = serverQuery.Where(p => p.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(restrictUserId))
            serverQuery = serverQuery.Where(p => p.RequestedBy == restrictUserId);

        var materialized = await serverQuery.ToListAsync(ct);

        IEnumerable<PettyCashRequest> filtered = materialized;
        if (restrictBranches is { Count: > 0 })
        {
            var allowed = new HashSet<string>(restrictBranches, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(p => allowed.Contains(p.Branch));
        }

        var ordered = filtered.OrderByDescending(p => p.RequestedDate).ToList();

        var total = ordered.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<PettyCashRequest>(items, total, page, pageSize);
    }

    public async Task<PettyCashRequest?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
