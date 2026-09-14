using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceLedger.Infrastructure.Persistence;

public static class DbInitializer
{
    private const string DefaultManagerEmail = "admin@financeledger.local";
    private const string DefaultManagerPassword = "Admin@123";

    /// <summary>
    /// Seeds baseline reference data. Schema itself is owned by the Supabase SQL
    /// migrations (supabase/migrations/*.sql), so this only touches rows, never DDL.
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

        var branches = await SeedBranchesAsync(context, ct);
        await SeedManagerAsync(context, identityProvider, branches, ct);
    }

    private static async Task<IReadOnlyList<Branch>> SeedBranchesAsync(LedgerDbContext context, CancellationToken ct)
    {
        var existing = await context.Branches.ToListAsync(ct);
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

    private static async Task SeedManagerAsync(
        LedgerDbContext context,
        IIdentityProviderService identityProvider,
        IReadOnlyList<Branch> branches,
        CancellationToken ct)
    {
        var users = await context.Users.ToListAsync(ct);
        var exists = users.Any(u => string.Equals(u.Email, DefaultManagerEmail, StringComparison.OrdinalIgnoreCase));
        if (exists)
            return;

        var branchIds = branches.Select(b => b.Id).ToArray();

        // The manager must exist in Supabase Auth to actually log in; the local row
        // mirrors the domain fields Supabase doesn't know about (display name, etc.).
        var identityUserId = await identityProvider.AdminCreateUserAsync(
            DefaultManagerEmail, DefaultManagerPassword, UserRole.Manager, branchIds, ct);

        var manager = new User
        {
            Id = identityUserId,
            Email = DefaultManagerEmail,
            DisplayName = "System Manager",
            Role = UserRole.Manager,
            AssignedBranches = branchIds,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await context.Users.AddAsync(manager, ct);
        await context.SaveChangesAsync(ct);
    }
}
