using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class ReleaseNoteRepository : RepositoryBase<ReleaseNote>, IReleaseNoteRepository
{
    public ReleaseNoteRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ReleaseNote>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        return await Set.OrderByDescending(r => r.PublishedDate).ToListAsync(ct);
    }

    public async Task<ReleaseNote?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
