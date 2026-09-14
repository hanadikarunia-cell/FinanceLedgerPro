using FinanceLedger.Application.Common;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IAuditRepository : IRepository<AuditLog>
{
    Task<PagedResult<AuditLog>> QueryAsync(int page, int pageSize, string? entity, string? userId, CancellationToken ct = default);
}
