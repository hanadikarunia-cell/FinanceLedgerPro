using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class CarRepository : RepositoryBase<Car>, ICarRepository
{
    public CarRepository(LedgerDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.ToListAsync(ct);
    }

    public async Task<Car?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
