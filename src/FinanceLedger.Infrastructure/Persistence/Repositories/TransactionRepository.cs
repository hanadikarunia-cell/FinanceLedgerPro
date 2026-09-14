using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Transaction>> QueryAsync(
        TransactionQuery query,
        IReadOnlyCollection<string>? restrictBranches,
        string? restrictUserId,
        CancellationToken ct = default)
    {
        // Apply the filters that translate cleanly to SQL server-side first.
        IQueryable<Transaction> serverQuery = Set.AsQueryable();

        if (query.Type.HasValue)
            serverQuery = serverQuery.Where(t => t.Type == query.Type.Value);

        if (!string.IsNullOrWhiteSpace(query.Category))
            serverQuery = serverQuery.Where(t => t.Category == query.Category);

        if (!string.IsNullOrWhiteSpace(query.Branch))
            serverQuery = serverQuery.Where(t => t.Branch == query.Branch);

        if (!string.IsNullOrWhiteSpace(query.UserId))
            serverQuery = serverQuery.Where(t => t.CreatedBy == query.UserId);

        if (query.Status.HasValue)
            serverQuery = serverQuery.Where(t => t.ApprovalStatus == query.Status.Value);

        if (query.From.HasValue)
            serverQuery = serverQuery.Where(t => t.Date >= query.From.Value);

        if (query.To.HasValue)
            serverQuery = serverQuery.Where(t => t.Date <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(restrictUserId))
            serverQuery = serverQuery.Where(t => t.CreatedBy == restrictUserId);

        var materialized = await serverQuery.ToListAsync(ct);

        // Filtered in memory rather than pushed into the SQL query - restrictBranches
        // comes from the current user's claims, not a query param, so this keeps the
        // branch-restriction logic identical regardless of provider.
        IEnumerable<Transaction> filtered = materialized;
        if (restrictBranches is { Count: > 0 })
        {
            var allowed = new HashSet<string>(restrictBranches, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(t => allowed.Contains(t.Branch));
        }

        var ordered = filtered.OrderByDescending(t => t.Date).ToList();

        var total = ordered.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Transaction>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        IReadOnlyCollection<string>? restrictBranches,
        CancellationToken ct = default)
    {
        var results = await Set
            .Where(t => t.Date >= from && t.Date <= to)
            .ToListAsync(ct);

        if (restrictBranches is { Count: > 0 })
        {
            var allowed = new HashSet<string>(restrictBranches, StringComparer.OrdinalIgnoreCase);
            results = results.Where(t => allowed.Contains(t.Branch)).ToList();
        }

        return results
            .OrderBy(t => t.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetApprovedNotSyncedAsync(CancellationToken ct = default)
    {
        return await Set
            .Where(t => t.ApprovalStatus == ApprovalStatus.Approved)
            .ToListAsync(ct);
    }

    public async Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
