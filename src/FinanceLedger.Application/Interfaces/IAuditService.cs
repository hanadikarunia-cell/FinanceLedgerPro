using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditAction action, string entity, string entityId, object? oldValue, object? newValue, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> QueryAsync(int page, int pageSize, string? entity, string? userId, CancellationToken ct = default);
}
