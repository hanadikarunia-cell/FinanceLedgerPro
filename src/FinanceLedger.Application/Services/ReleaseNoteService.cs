using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Services;

public class ReleaseNoteService : IReleaseNoteService
{
    private readonly IReleaseNoteRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public ReleaseNoteService(IReleaseNoteRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ReleaseNoteDto>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllOrderedAsync(ct);
        return all.Select(r => r.ToDto()).ToList();
    }

    public async Task<ReleaseNoteDto> CreateAsync(CreateReleaseNoteDto dto, CancellationToken ct = default)
    {
        EnsureManager();

        var entity = new ReleaseNote
        {
            Version = dto.Version,
            Title = dto.Title,
            Type = dto.Type,
            Notes = dto.Notes,
            PublishedBy = _currentUser.UserId ?? "unknown",
            PublishedByName = _currentUser.UserName ?? _currentUser.Email ?? "unknown",
            PublishedDate = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(entity, ct);
        return created.ToDto();
    }

    public async Task<ReleaseNoteDto> UpdateAsync(string id, UpdateReleaseNoteDto dto, CancellationToken ct = default)
    {
        EnsureManager();

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(ReleaseNote), id);

        entity.Version = dto.Version;
        entity.Title = dto.Title;
        entity.Type = dto.Type;
        entity.Notes = dto.Notes;

        var updated = await _repo.UpdateAsync(entity, ct);
        return updated.ToDto();
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        EnsureManager();

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(ReleaseNote), id);

        await _repo.DeleteAsync(entity, ct);
    }

    private void EnsureManager()
    {
        if (!_currentUser.IsAppAdmin)
            throw new ForbiddenException("Only the Application Admin can publish What's New updates.");
    }
}
