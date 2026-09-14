using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IReleaseNoteRepository : IRepository<ReleaseNote>
{
    Task<IReadOnlyList<ReleaseNote>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<ReleaseNote?> GetByIdAsync(string id, CancellationToken ct = default);
}
