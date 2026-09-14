using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IPettyCashRequestRepository : IRepository<PettyCashRequest>
{
    Task<PagedResult<PettyCashRequest>> QueryAsync(
        PettyCashRequestQuery query,
        IReadOnlyCollection<string>? restrictBranches,
        string? restrictUserId,
        CancellationToken ct = default);

    Task<PettyCashRequest?> GetByIdAsync(string id, CancellationToken ct = default);
}
