using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence;

public static class DbInitializer
{
    // Fixed ids so the SQL migration, the seeder and the Worker all agree on them.
    public const string Client1TenantId = "00000000-0000-0000-0000-000000000001";
    public const string Client2TenantId = "00000000-0000-0000-0000-000000000002";

    private const string AppAdminEmail = "admin@financeledger.local";
    private const string AppAdminPassword = "Admin@123";
    private const string Client1AdminEmail = "client1.admin@financeledger.local";
    private const string Client1AdminPassword = "Admin@123";

    /// <summary>
    /// Seeds baseline reference data on an empty database: the Client 1 branches, its Site
    /// Admin, and the Application Admin. Schema itself (and the two tenant rows) is owned
    /// by the Supabase SQL migrations (supabase/migrations/*.sql), so this only touches
    /// rows, never DDL. Does nothing once the Application Admin's email exists.
    /// The context has no site here (no request), so every query steps outside the
    /// site filter explicitly and every row names its site.
    /// </summary>
    public static async Task EnsureSeedDataAsync(
        LedgerDbContext context,
        IIdentityProviderService identityProvider,
        CancellationToken ct = default)
    {
        if (!await context.Database.CanConnectAsync(ct))
        {
            return;
        }

        if (!await context.Tenants.AnyAsync(t => t.Id == Client1TenantId, ct))
        {
            // Migration 0004 seeds the tenants; without it the schema is out of date.
            return;
        }

        var branches = await SeedBranchesAsync(context, ct);
        await SeedAdminsAsync(context, identityProvider, branches, ct);
    }

    private static async Task<IReadOnlyList<Branch>> SeedBranchesAsync(LedgerDbContext context, CancellationToken ct)
    {
        var existing = await context.Branches.IgnoreQueryFilters()
            .Where(b => b.TenantId == Client1TenantId)
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);

        var desired = new (string Name, string Code)[]
        {
            ("Jakarta", "JKT"),
            ("Surabaya", "SBY"),
            ("Bandung", "BDG")
        };

        var result = new List<Branch>();
        var added = false;

        foreach (var (name, code) in desired)
        {
            if (byCode.TryGetValue(code, out var found))
            {
                result.Add(found);
                continue;
            }

            var branch = new Branch
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = Client1TenantId,
                Name = name,
                Code = code,
                Address = string.Empty,
                IsActive = true
            };

            await context.Branches.AddAsync(branch, ct);
            result.Add(branch);
            added = true;
        }

        if (added)
            await context.SaveChangesAsync(ct);

        return result;
    }

    private static async Task SeedAdminsAsync(
        LedgerDbContext context,
        IIdentityProviderService identityProvider,
        IReadOnlyList<Branch> branches,
        CancellationToken ct)
    {
        var users = await context.Users.IgnoreQueryFilters().ToListAsync(ct);
        bool Has(string email) => users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        // A database that already has the admin account is already set up (an older
        // deployment's admin is promoted to Application Admin by the 0004 data step).
        if (Has(AppAdminEmail))
            return;

        var branchIds = branches.Select(b => b.Id).ToArray();

        // Both must exist in Supabase Auth to actually log in; the local rows mirror the
        // domain fields Supabase doesn't know about (display name, site, etc.).
        var siteAdminId = await identityProvider.AdminCreateUserAsync(
            Client1AdminEmail, Client1AdminPassword, UserRole.Manager, branchIds, ct);
        await context.Users.AddAsync(new User
        {
            Id = siteAdminId,
            TenantId = Client1TenantId,
            Email = Client1AdminEmail,
            DisplayName = "Client 1 Admin",
            Role = UserRole.Manager,
            AssignedBranches = branchIds,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        }, ct);

        var appAdminId = await identityProvider.AdminCreateUserAsync(
            AppAdminEmail, AppAdminPassword, UserRole.AppAdmin, Array.Empty<string>(), ct);
        await context.Users.AddAsync(new User
        {
            Id = appAdminId,
            TenantId = string.Empty,
            Email = AppAdminEmail,
            DisplayName = "Application Admin",
            Role = UserRole.AppAdmin,
            AssignedBranches = Array.Empty<string>(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        }, ct);

        await context.SaveChangesAsync(ct);
    }
}
