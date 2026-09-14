using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Worker.Services;

/// <summary>
/// Represents the background "system" principal used by the sync worker.
/// The worker runs outside of any HTTP request, so there is no authenticated
/// user; Application services still require an <see cref="ICurrentUserService"/>
/// to resolve, so this no-op implementation supplies a system identity with
/// manager-level access and no branch restrictions.
/// </summary>
public sealed class SystemCurrentUserService : ICurrentUserService
{
    public string? UserId => "system";

    public string? UserName => "System Sync Worker";

    public string? Email => null;

    public UserRole? Role => UserRole.Manager;

    public IReadOnlyCollection<string> AssignedBranches => Array.Empty<string>();

    public bool IsManager => true;

    public bool IsAuthenticated => false;
}
