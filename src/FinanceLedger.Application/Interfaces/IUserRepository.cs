using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    // The plain methods below are confined to the current site by the DbContext's
    // tenant filter. The *AnyTenant methods deliberately look across all sites and exist
    // only for things that happen before/outside a site context: signing in (the token
    // knows the user, not yet the site), resolving who is acting as whom, email
    // uniqueness (Supabase Auth emails are global), and Application Admin site management.
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    Task<User?> GetByEmailAnyTenantAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAnyTenantAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByTenantAnyTenantAsync(string tenantId, CancellationToken ct = default);
    Task<int> CountByTenantAnyTenantAsync(string tenantId, CancellationToken ct = default);
}
