using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public FeedbackService(IFeedbackRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FeedbackDto>> QueryAsync(FeedbackQuery query, CancellationToken ct = default)
    {
        // Feedback goes to the application owner, across all sites: only the Application
        // Admin sees and triages everyone's; everyone else sees their own submissions.
        var restrictUserId = _currentUser.IsAppAdmin ? null : _currentUser.UserId;

        var page = await _repo.QueryAsync(query, restrictUserId, ct);
        return new PagedResult<FeedbackDto>(
            page.Items.Select(x => x.ToDto()).ToList(),
            page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, CancellationToken ct = default)
    {
        var entity = new Feedback
        {
            Message = dto.Message,
            ImageUrl = dto.ImageUrl,
            AppVersion = dto.AppVersion,
            SubmittedBy = _currentUser.UserId ?? "unknown",
            SubmittedByName = _currentUser.UserName ?? _currentUser.Email ?? "unknown",
            SubmittedDate = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(entity, ct);
        return created.ToDto();
    }

    public async Task<FeedbackDto> SetSeverityAsync(string id, FeedbackSeverity severity, CancellationToken ct = default)
    {
        if (!_currentUser.IsAppAdmin)
            throw new ForbiddenException("Only the Application Admin can triage feedback.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);

        entity.Severity = severity;
        var updated = await _repo.UpdateAsync(entity, ct);
        return updated.ToDto();
    }
}
