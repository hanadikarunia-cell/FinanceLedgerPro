using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<PagedResult<Feedback>> QueryAsync(
        FeedbackQuery query,
        string? restrictUserId,
        CancellationToken ct = default);

    Task<Feedback?> GetByIdAsync(string id, CancellationToken ct = default);
}
