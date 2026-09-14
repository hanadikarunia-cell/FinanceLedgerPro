using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface ICarRepository : IRepository<Car>
{
    Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken ct = default);
    Task<Car?> GetByIdAsync(string id, CancellationToken ct = default);
}
