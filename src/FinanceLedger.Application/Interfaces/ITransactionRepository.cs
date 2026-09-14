using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<PagedResult<Transaction>> QueryAsync(TransactionQuery query, IReadOnlyCollection<string>? restrictBranches, string? restrictUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to, IReadOnlyCollection<string>? restrictBranches, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetApprovedNotSyncedAsync(CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default);
}
