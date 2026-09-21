using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<IReadOnlyList<Tenant>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Tenant?> GetByCodeAsync(string code, CancellationToken ct = default);
}
