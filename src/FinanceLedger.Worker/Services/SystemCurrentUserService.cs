using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Worker.Services;

/// <summary>
/// Represents the background "system" principal used by the sync worker.
/// The worker runs outside of any HTTP request, so there is no authenticated
/// user; Application services still require an <see cref="ICurrentUserService"/>
/// to resolve, so this no-op implementation supplies a system identity with
/// manager-level access and no branch restrictions. It is bound to a single site
/// (the one whose data is synced), configured via Sync:TenantId.
/// </summary>
public sealed class SystemCurrentUserService : ICurrentUserService, ITenantProvider
{
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001"; // Client 1

    private readonly string _tenantId;

    public SystemCurrentUserService(IConfiguration configuration)
    {
        var configured = configuration["Sync:TenantId"];
        _tenantId = string.IsNullOrWhiteSpace(configured) ? DefaultTenantId : configured;
    }

    public string? UserId => "system";

    public string? UserName => "System Sync Worker";

    public string? Email => null;

    public UserRole? Role => UserRole.Manager;

    public IReadOnlyCollection<string> AssignedBranches => Array.Empty<string>();

    public bool IsManager => true;

    public bool IsAppAdmin => false;

    public bool IsAuthenticated => false;

    public string? TenantId => _tenantId;

    public bool IsActingAs => false;

    public string? ActingAdminId => null;

    public string? ActingAdminName => null;

    public bool ActingCanWrite => true;
}
