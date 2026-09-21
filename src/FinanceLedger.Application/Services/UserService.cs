using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly IIdentityProviderService _identityProvider;

    public UserService(
        IUserRepository repo,
        ICurrentUserService currentUser,
        IAuditService audit,
        IIdentityProviderService identityProvider)
    {
        _repo = repo;
        _currentUser = currentUser;
        _audit = audit;
        _identityProvider = identityProvider;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureManager();
        var users = await _repo.GetAllAsync(ct);
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        EnsureManager();

        EnsureAssignableRole(dto.Role);

        // Emails are unique across all sites (they are the login), so check every site.
        var existing = await _repo.GetByEmailAnyTenantAsync(dto.Email, ct);
        if (existing is not null)
            throw new ConflictException($"A user with email '{dto.Email}' already exists.");

        var identityUserId = await _identityProvider.AdminCreateUserAsync(
            dto.Email, dto.Password, dto.Role, dto.AssignedBranches, ct);

        var user = new User
        {
            Id = identityUserId,
            TenantId = _currentUser.TenantId ?? string.Empty,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            Role = dto.Role,
            AssignedBranches = dto.AssignedBranches,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(user, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(User), created.Id, null, created.ToDto(), ct);
        return created.ToDto();
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default)
    {
        EnsureManager();

        EnsureAssignableRole(dto.Role);

        var user = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        var old = user.ToDto();
        user.DisplayName = dto.DisplayName;
        user.Role = dto.Role;
        user.AssignedBranches = dto.AssignedBranches;
        user.IsActive = dto.IsActive;

        // Keep app_metadata (role/branches) in sync so the next token Supabase issues
        // for this user carries the updated RBAC claims.
        await _identityProvider.AdminUpdateUserAsync(id, dto.Role, dto.AssignedBranches, ct: ct);

        var updated = await _repo.UpdateAsync(user, ct);
        await _audit.LogAsync(AuditAction.Update, nameof(User), id, old, updated.ToDto(), ct);
        return updated.ToDto();
    }

    public async Task ResetPasswordAsync(string id, string newPassword, CancellationToken ct = default)
    {
        EnsureManager();

        var user = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        await _identityProvider.AdminUpdateUserAsync(user.Id, password: newPassword, ct: ct);
        await _audit.LogAsync(AuditAction.Update, nameof(User), id, null, new { PasswordReset = true }, ct);
    }

    private void EnsureManager()
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Site Admins can manage users.");
    }

    // The Application Admin is not a per-site role: it is created only by the seeder
    // (and by hand), never through the site's user management.
    private static void EnsureAssignableRole(UserRole role)
    {
        if (role == UserRole.AppAdmin)
            throw new ForbiddenException("The Application Admin role cannot be assigned here.");
    }
}
