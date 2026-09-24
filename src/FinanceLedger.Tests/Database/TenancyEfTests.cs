using FinanceLedger.Application.Common;
using FinanceLedger.Application.Services;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using FinanceLedger.Infrastructure.Persistence.Repositories;
using FinanceLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceLedger.Tests.Database;

/// <summary>
/// The .NET side of tenant isolation - the EF query filter, save-time stamping, the connection
/// interceptor and the bypass scope - against a real database as the restricted app_api role,
/// i.e. both isolation layers together, the way production runs.
/// </summary>
[Collection(PostgresCollection.Name)]
public class TenancyEfTests
{
    private const string A = PostgresFixture.Client1;
    private const string B = PostgresFixture.Client2;

    private readonly PostgresFixture _pg;

    public TenancyEfTests(PostgresFixture pg) => _pg = pg;

    private async Task<string> SeedAsync()
    {
        var db = await _pg.CreateDatabaseAsync();
        await _pg.ExecuteAsync(db, $@"
            insert into branches (id, name, code, tenant_id) values ('a-branch', 'Jakarta', 'JKT', '{A}');
            insert into branches (id, name, code, tenant_id) values ('b-branch', 'Medan', 'MDN', '{B}');
            insert into users (id, email, role, tenant_id) values ('a-user', 'a@x.test', 'Manager', '{A}');
            insert into users (id, email, role, tenant_id) values ('b-user', 'b@x.test', 'User', '{B}');
            insert into users (id, email, role, tenant_id) values ('root', 'root@x.test', 'AppAdmin', '');
            insert into transactions (id, type, amount, transaction_date, branch, tenant_id) values ('a-tx', 'Income', 5, now(), 'JKT', '{A}');
            insert into transactions (id, type, amount, transaction_date, branch, tenant_id) values ('b-tx', 'Income', 7, now(), 'MDN', '{B}');");
        return db;
    }

    // ---- both layers -----------------------------------------------------------------

    [Fact]
    public async Task Repositories_return_only_the_callers_site()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        var branches = await new BranchRepository(ctx).GetAllAsync();
        Assert.Equal(new[] { "a-branch" }, branches.Select(b => b.Id));

        var tx = await new TransactionRepository(ctx).GetByIdAsync("a-tx");
        Assert.NotNull(tx);
    }

    [Fact]
    public async Task Another_sites_record_by_id_is_not_found()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        // The IDOR case: a valid id, but it belongs to site B.
        Assert.Null(await new TransactionRepository(ctx).GetByIdAsync("b-tx"));
        Assert.Null(await new BranchRepository(ctx).GetByIdAsync("b-branch", string.Empty));
    }

    [Fact]
    public async Task The_database_alone_still_isolates_when_the_EF_filter_is_switched_off()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        // IgnoreQueryFilters removes the application-level filter; row-level security must still hold.
        var rows = await ctx.Transactions.IgnoreQueryFilters().Select(t => t.Id).ToListAsync();
        Assert.Equal(new[] { "a-tx" }, rows);
    }

    [Fact]
    public async Task A_caller_with_no_site_sees_no_site_data()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, (string?)null);

        Assert.Empty(await ctx.Transactions.ToListAsync());
        Assert.Empty(await ctx.Branches.IgnoreQueryFilters().ToListAsync());
    }

    // ---- writes ----------------------------------------------------------------------

    [Fact]
    public async Task New_rows_are_stamped_with_the_callers_site()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        var created = await new BranchRepository(ctx).AddAsync(new Branch { Id = "new", Name = "Bandung", Code = "BDG" });

        Assert.Equal(A, created.TenantId);
        Assert.Equal(A, await _pg.ScalarAsync<string>(db, "select tenant_id from branches where id = 'new'"));
    }

    [Fact]
    public async Task A_row_cannot_be_written_into_another_site_without_bypass()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new BranchRepository(ctx).AddAsync(new Branch { Id = "evil", Name = "X", Code = "X", TenantId = B }));

        Assert.Equal(0L, await _pg.ScalarAsync<long>(db, "select count(*) from branches where id = 'evil'"));
    }

    [Fact]
    public async Task A_bypass_scope_may_create_a_row_in_another_site()
    {
        // This is what creating a site does: the Application Admin writes the new site's first branch.
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, (string?)null);

        using (TenantBypassContext.Begin())
        {
            await new BranchRepository(ctx).AddAsync(new Branch { Id = "main", Name = "Main", Code = "MAIN", TenantId = B });
        }

        Assert.Equal(B, await _pg.ScalarAsync<string>(db, "select tenant_id from branches where id = 'main'"));
    }

    [Fact]
    public async Task Saving_without_any_site_is_refused_for_business_data()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, (string?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new BranchRepository(ctx).AddAsync(new Branch { Id = "orphan", Name = "X", Code = "X" }));
    }

    [Fact]
    public async Task An_existing_rows_site_cannot_be_changed_through_EF()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);
        var repo = new BranchRepository(ctx);

        var branch = (await repo.GetAllAsync()).Single();
        branch.TenantId = B;
        branch.Name = "Renamed";
        await repo.UpdateAsync(branch);

        Assert.Equal(A, await _pg.ScalarAsync<string>(db, "select tenant_id from branches where id = 'a-branch'"));
        Assert.Equal("Renamed", await _pg.ScalarAsync<string>(db, "select name from branches where id = 'a-branch'"));
    }

    [Fact]
    public async Task Update_and_delete_of_another_sites_row_do_nothing()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        // Smuggle site B's row into the change tracker as if a bug had loaded it.
        var foreign = new Branch { Id = "b-branch", Name = "Hijacked", Code = "MDN", TenantId = B };
        ctx.Branches.Attach(foreign);
        ctx.Entry(foreign).State = EntityState.Modified;

        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
        Assert.Equal("Medan", await _pg.ScalarAsync<string>(db, "select name from branches where id = 'b-branch'"));
    }

    // ---- cross-site lookups ----------------------------------------------------------

    [Fact]
    public async Task Login_style_user_lookups_see_across_sites_through_the_bypass()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);
        var users = new UserRepository(ctx);

        Assert.NotNull(await users.GetByIdAnyTenantAsync("b-user"));
        Assert.NotNull(await users.GetByEmailAnyTenantAsync("B@X.TEST"));
        Assert.NotNull(await users.GetByIdAnyTenantAsync("root"));
        Assert.Equal(1, await users.CountByTenantAnyTenantAsync(B));
        Assert.Single(await users.GetByTenantAnyTenantAsync(B));
    }

    [Fact]
    public async Task Ordinary_user_queries_stay_inside_the_site()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);
        var users = new UserRepository(ctx);

        Assert.Null(await users.GetByIdAsync("b-user"));
        Assert.Null(await users.GetByEmailAsync("b@x.test"));
        Assert.Equal(new[] { "a-user" }, (await users.GetAllAsync()).Select(u => u.Id));
    }

    [Fact]
    public async Task Bypass_never_leaks_into_the_next_request_on_a_pooled_connection()
    {
        var db = await SeedAsync();

        // Same physical connection is reused (pooling on) - alternate a bypass lookup with an
        // ordinary query and make sure the ordinary one never inherits the bypass.
        for (var i = 0; i < 10; i++)
        {
            await using (var ctx = Db.Context(_pg, db, A, pooling: true))
            {
                Assert.NotNull(await new UserRepository(ctx).GetByIdAnyTenantAsync("b-user"));
            }

            await using (var ctx = Db.Context(_pg, db, A, pooling: true))
            {
                var ids = await ctx.Transactions.IgnoreQueryFilters().Select(t => t.Id).ToListAsync();
                Assert.Equal(new[] { "a-tx" }, ids);
            }

            await using (var ctx = Db.Context(_pg, db, B, pooling: true))
            {
                var ids = await ctx.Transactions.IgnoreQueryFilters().Select(t => t.Id).ToListAsync();
                Assert.Equal(new[] { "b-tx" }, ids);
            }
        }
    }

    // ---- tenants table ---------------------------------------------------------------

    [Fact]
    public async Task Tenant_repository_works_for_any_caller_through_its_own_bypass()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);
        var tenants = new TenantRepository(ctx);

        Assert.Equal(new[] { "Client 1", "Client 2" }, (await tenants.GetAllOrderedAsync()).Select(t => t.Name));
        Assert.NotNull(await tenants.GetByIdAsync(B));
        Assert.NotNull(await tenants.GetByCodeAsync("client2"));

        var created = await tenants.AddAsync(new Tenant { Id = "t3", Name = "Client 3", Code = "C3" });
        Assert.Equal("t3", created.Id);
    }

    [Fact]
    public async Task A_site_context_outside_the_repository_sees_only_its_own_tenant_and_cannot_write()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, A);

        Assert.Equal(new[] { A }, (await ctx.Tenants.ToListAsync()).Select(t => t.Id));

        ctx.Tenants.Add(new Tenant { Id = "t9", Name = "Sneaky", Code = "SNEAK" });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ---- impersonation sessions ------------------------------------------------------

    [Fact]
    public async Task Impersonation_sessions_round_trip_through_the_repository()
    {
        var db = await SeedAsync();
        await using var ctx = Db.Context(_pg, db, (string?)null);
        var repo = new ImpersonationSessionRepository(ctx);
        var now = DateTime.UtcNow;

        var session = await repo.AddAsync(new ImpersonationSession
        {
            AdminUserId = "root",
            TargetUserId = "b-user",
            TargetTenantId = B,
            StartedAt = now,
            ExpiresAt = now.AddHours(1),
            IpAddress = "10.0.0.1"
        });

        var loaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.True(loaded!.IsActive(now));
        Assert.Single(await repo.GetActiveForAdminAsync("root", now));

        loaded.EndedAt = now;
        await repo.UpdateAsync(loaded);
        Assert.Empty(await repo.GetActiveForAdminAsync("root", now));
        Assert.False((await repo.GetByIdAsync(session.Id))!.IsActive(now));
    }

    [Fact]
    public async Task An_expired_session_is_not_active()
    {
        var session = new ImpersonationSession { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        Assert.False(session.IsActive(DateTime.UtcNow));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Sessions_cannot_be_read_from_a_site_context_directly()
    {
        var db = await SeedAsync();
        await _pg.ExecuteAsync(db, $"insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, expires_at) " +
                                   $"values ('s1', 'root', 'b-user', '{B}', now() + interval '1 hour')");

        await using var ctx = Db.Context(_pg, db, B);
        Assert.Empty(await ctx.ImpersonationSessions.ToListAsync());
    }

    // ---- audit -----------------------------------------------------------------------

    [Fact]
    public async Task Audit_records_the_real_actor_and_the_person_they_were_viewing_as()
    {
        var db = await SeedAsync();
        var caller = new FakeCurrentUser
        {
            UserId = "b-user",                 // effective identity: the person being viewed
            UserName = "Bea",
            Role = UserRole.User,
            TenantId = B,
            IsActingAs = true,
            ActingAdminId = "root",            // the real, signed-in admin
            ActingAdminName = "Application Admin"
        };

        await using var ctx = Db.Context(_pg, db, caller);
        await new AuditService(new AuditRepository(ctx), caller)
            .LogAsync(AuditAction.Create, "Transaction", "b-tx", null, new { Amount = 1 });

        Assert.Equal("root", await _pg.ScalarAsync<string>(db, "select actor_user_id from audit_logs limit 1"));
        Assert.Equal("b-user", await _pg.ScalarAsync<string>(db, "select acting_as_user_id from audit_logs limit 1"));
        Assert.Equal("b-user", await _pg.ScalarAsync<string>(db, "select user_id from audit_logs limit 1"));
        Assert.Equal(B, await _pg.ScalarAsync<string>(db, "select tenant_id from audit_logs limit 1"));
    }

    [Fact]
    public async Task Audit_of_a_normal_action_has_the_actor_and_no_acting_as()
    {
        var db = await SeedAsync();
        var caller = new FakeCurrentUser { UserId = "a-user", Role = UserRole.Manager, TenantId = A };

        await using var ctx = Db.Context(_pg, db, caller);
        await new AuditService(new AuditRepository(ctx), caller)
            .LogAsync(AuditAction.Update, "Branch", "a-branch", null, null);

        Assert.Equal("a-user", await _pg.ScalarAsync<string>(db, "select actor_user_id from audit_logs limit 1"));
        Assert.Null(await _pg.ScalarAsync<string>(db, "select acting_as_user_id from audit_logs limit 1"));
    }

    [Fact]
    public async Task One_sites_audit_log_is_invisible_to_another()
    {
        var db = await SeedAsync();
        var callerA = new FakeCurrentUser { UserId = "a-user", Role = UserRole.Manager, TenantId = A };
        await using (var ctxA = Db.Context(_pg, db, callerA))
            await new AuditService(new AuditRepository(ctxA), callerA).LogAsync(AuditAction.Create, "Branch", "x", null, null);

        var callerB = new FakeCurrentUser { UserId = "b-user", Role = UserRole.Manager, TenantId = B };
        await using var ctxB = Db.Context(_pg, db, callerB);
        var page = await new AuditService(new AuditRepository(ctxB), callerB).QueryAsync(1, 20, null, null);

        Assert.Empty(page.Items);
    }
}
