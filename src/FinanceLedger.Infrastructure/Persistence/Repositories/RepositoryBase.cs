using System.Linq.Expressions;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : class, IEntity
{
    protected readonly LedgerDbContext Context;
    protected readonly DbSet<T> Set;

    public RepositoryBase(LedgerDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(string id, string partitionKey, CancellationToken ct = default)
    {
        // FirstOrDefaultAsync (not FindAsync): Find returns tracked entities from the identity
        // map without consulting the site query filter.
        return await Set.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
    {
        return await Set.ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await Set.Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        Set.Update(entity);
        try
        {
            await Context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                $"{typeof(T).Name} '{entity.Id}' was modified by someone else. Reload and try again.");
        }

        return entity;
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        Set.Remove(entity);
        await Context.SaveChangesAsync(ct);
    }
}
