using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : RepositoryBase<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Invoice>> QueryAsync(
        InvoiceQuery query,
        IReadOnlyCollection<string>? restrictBranches,
        CancellationToken ct = default)
    {
        IQueryable<Invoice> serverQuery = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Branch))
            serverQuery = serverQuery.Where(i => i.Branch == query.Branch);

        if (query.Type.HasValue)
            serverQuery = serverQuery.Where(i => i.Type == query.Type.Value);

        if (query.Status.HasValue)
            serverQuery = serverQuery.Where(i => i.Status == query.Status.Value);

        var materialized = await serverQuery.ToListAsync(ct);

        IEnumerable<Invoice> filtered = materialized;
        if (restrictBranches is { Count: > 0 })
        {
            var allowed = new HashSet<string>(restrictBranches, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(i => allowed.Contains(i.Branch));
        }

        var ordered = filtered.OrderByDescending(i => i.InvoiceDate).ToList();

        var total = ordered.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Invoice>(items, total, page, pageSize);
    }

    public async Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(i => i.Id == id, ct);
    }
}
