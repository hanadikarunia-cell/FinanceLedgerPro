using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class AttachmentRepository : RepositoryBase<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<Attachment?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(a => a.Id == id, ct);
    }
}
