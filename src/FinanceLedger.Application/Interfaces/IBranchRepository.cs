using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IBranchRepository : IRepository<Branch>
{
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct = default);
    Task<Branch?> GetByCodeAsync(string code, CancellationToken ct = default);
}
