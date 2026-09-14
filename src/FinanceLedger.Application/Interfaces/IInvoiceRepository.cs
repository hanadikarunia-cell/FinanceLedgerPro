using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<PagedResult<Invoice>> QueryAsync(
        InvoiceQuery query,
        IReadOnlyCollection<string>? restrictBranches,
        CancellationToken ct = default);

    Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default);
}
