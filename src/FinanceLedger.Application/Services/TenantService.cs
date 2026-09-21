using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenants;
    private readonly IUserRepository _users;
    private readonly IBranchRepository _branches;
    private readonly IIdentityProviderService _identityProvider;
    private readonly ICurrentUserService _currentUser;

    public TenantService(
        ITenantRepository tenants,
        IUserRepository users,
        IBranchRepository branches,
        IIdentityProviderService identityProvider,
        ICurrentUserService currentUser)
    {
        _tenants = tenants;
        _users = users;
        _branches = branches;
        _identityProvider = identityProvider;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureAppAdmin();

        var tenants = await _tenants.GetAllOrderedAsync(ct);
        var result = new List<TenantDto>(tenants.Count);
        foreach (var tenant in tenants)
        {
            var count = await _users.CountByTenantAnyTenantAsync(tenant.Id, ct);
            result.Add(tenant.ToDto(count));
        }

        return result;
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default)
    {
        EnsureAppAdmin();

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _tenants.GetByCodeAsync(code, ct) is not null)
            throw new ConflictException($"A site with code '{code}' already exists.");

        if (await _users.GetByEmailAnyTenantAsync(dto.AdminEmail, ct) is not null)
            throw new ConflictException($"A user with email '{dto.AdminEmail}' already exists.");

        var tenant = new Tenant { Name = dto.Name.Trim(), Code = code };

        // A new site starts with one branch so its admin can record transactions straight away.
        var branch = new Branch { TenantId = tenant.Id, Name = "Main", Code = "MAIN", IsActive = true };

        // The external identity goes first: if Supabase rejects it nothing local has been written.
        var identityUserId = await _identityProvider.AdminCreateUserAsync(
            dto.AdminEmail, dto.AdminPassword, UserRole.Manager, new[] { branch.Id }, ct);

        await _tenants.AddAsync(tenant, ct);
        await _branches.AddAsync(branch, ct);
        await _users.AddAsync(new User
        {
            Id = identityUserId,
            TenantId = tenant.Id,
            Email = dto.AdminEmail,
            DisplayName = dto.AdminDisplayName,
            Role = UserRole.Manager,
            AssignedBranches = new[] { branch.Id },
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        }, ct);

        return tenant.ToDto(1);
    }

    public async Task<TenantDto> UpdateAsync(string id, UpdateTenantDto dto, CancellationToken ct = default)
    {
        EnsureAppAdmin();

        var tenant = await _tenants.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Tenant), id);

        tenant.Name = dto.Name.Trim();
        tenant.IsActive = dto.IsActive;

        var updated = await _tenants.UpdateAsync(tenant, ct);
        return updated.ToDto(await _users.CountByTenantAnyTenantAsync(id, ct));
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string tenantId, CancellationToken ct = default)
    {
        EnsureAppAdmin();

        if (await _tenants.GetByIdAsync(tenantId, ct) is null)
            throw new NotFoundException(nameof(Tenant), tenantId);

        var users = await _users.GetByTenantAnyTenantAsync(tenantId, ct);
        return users.Select(u => u.ToDto()).ToList();
    }

    private void EnsureAppAdmin()
    {
        if (!_currentUser.IsAppAdmin)
            throw new ForbiddenException("Only the Application Admin can manage sites.");
    }
}
