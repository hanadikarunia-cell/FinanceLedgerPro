using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class FeedbackRepository : RepositoryBase<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Feedback>> QueryAsync(
        FeedbackQuery query,
        string? restrictUserId,
        CancellationToken ct = default)
    {
        IQueryable<Feedback> serverQuery = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(restrictUserId))
            serverQuery = serverQuery.Where(f => f.SubmittedBy == restrictUserId);

        var ordered = await serverQuery.OrderByDescending(f => f.SubmittedDate).ToListAsync(ct);

        var total = ordered.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Feedback>(items, total, page, pageSize);
    }

    public async Task<Feedback?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}
