using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<Attachment?> GetByIdAsync(string id, CancellationToken ct = default);
}
