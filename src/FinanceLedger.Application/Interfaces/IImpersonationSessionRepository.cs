using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

/// <summary>
/// Impersonation sessions are cross-site by nature (an Application Admin starts one for a
/// person in any site), so every method here runs in a bypass scope.
/// </summary>
public interface IImpersonationSessionRepository : IRepository<ImpersonationSession>
{
    Task<ImpersonationSession?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Sessions of this admin that have neither ended nor expired.</summary>
    Task<IReadOnlyList<ImpersonationSession>> GetActiveForAdminAsync(string adminUserId, DateTime nowUtc, CancellationToken ct = default);
}
