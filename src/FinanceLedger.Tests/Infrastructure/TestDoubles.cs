using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Enums;
using FinanceLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Tests.Infrastructure;

public sealed class FakeTenantProvider : ITenantProvider
{
    public FakeTenantProvider(string? tenantId) => TenantId = tenantId;
    public string? TenantId { get; set; }
}

/// <summary>A caller with whatever identity a test needs; stands in for the HTTP request.</summary>
public sealed class FakeCurrentUser : ICurrentUserService, ITenantProvider
{
    public string? UserId { get; set; } = "user";
    public string? UserName { get; set; } = "User Name";
    public string? Email { get; set; } = "user@test.local";
    public UserRole? Role { get; set; } = UserRole.User;
    public IReadOnlyCollection<string> AssignedBranches { get; set; } = Array.Empty<string>();
    public bool IsManager => Role == UserRole.Manager;
    public bool IsAppAdmin => Role == UserRole.AppAdmin;
    public bool IsAuthenticated => true;
    public string? TenantId { get; set; }
    public bool IsActingAs { get; set; }
    public string? ActingAdminId { get; set; }
    public string? ActingAdminName { get; set; }
    public bool ActingCanWrite => false;
}

public static class Db
{
    /// <summary>A context wired exactly like production: restricted role + the tenant session interceptor.</summary>
    public static LedgerDbContext Context(PostgresFixture pg, string db, ITenantProvider provider, bool pooling = false)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseNpgsql(pg.AppConnectionString(db, pooling))
            .AddInterceptors(new TenantSessionInterceptor(provider))
            .Options;
        return new LedgerDbContext(options, provider);
    }

    public static LedgerDbContext Context(PostgresFixture pg, string db, string? tenantId, bool pooling = false) =>
        Context(pg, db, new FakeTenantProvider(tenantId), pooling);
}
