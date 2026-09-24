using System.Text.Json;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    private readonly ICurrentUserService _currentUser;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditService(IAuditRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task LogAsync(AuditAction action, string entity, string entityId, object? oldValue, object? newValue, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.UserId ?? "system",
            // The real, signed-in person, even while they are viewing as someone else.
            ActorUserId = _currentUser.ActingAdminId ?? _currentUser.UserId,
            ActingAsUserId = _currentUser.IsActingAs ? _currentUser.UserId : null,
            // While an admin acts as someone, the entry is attributed to the acted-as user
            // (whose data it is) but names the real admin so the trail stays honest.
            UserName = (_currentUser.UserName ?? "system")
                + (_currentUser.IsActingAs ? $" (by {_currentUser.ActingAdminName ?? "admin"})" : string.Empty),
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
            Timestamp = DateTime.UtcNow
        };
        await _repo.AddAsync(log, ct);
    }

    public async Task<PagedResult<AuditLogDto>> QueryAsync(int page, int pageSize, string? entity, string? userId, CancellationToken ct = default)
    {
        var result = await _repo.QueryAsync(page, pageSize, entity, userId, ct);
        return new PagedResult<AuditLogDto>(
            result.Items.Select(x => x.ToDto()).ToList(),
            result.TotalCount, result.Page, result.PageSize);
    }
}
