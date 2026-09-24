using FinanceLedger.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace FinanceLedger.Tests.Database;

[Collection(PostgresCollection.Name)]
public class MigrationTests
{
    private readonly PostgresFixture _pg;

    public MigrationTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public void Migrations_are_present_in_order()
    {
        var names = PostgresFixture.MigrationFiles().Select(Path.GetFileName).ToList();
        Assert.Equal("0001_init.sql", names[0]);
        Assert.Contains("0004_multitenancy.sql", names);
        Assert.Contains("0006_audit_and_impersonation.sql", names);
        Assert.Contains("0007_tenant_hardening.sql", names);
    }

    [Fact]
    public async Task All_migrations_apply_cleanly_to_an_empty_database()
    {
        var db = await _pg.CreateEmptyDatabaseAsync();
        foreach (var file in PostgresFixture.MigrationFiles())
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));

        var tables = await _pg.ScalarAsync<long>(db,
            "select count(*) from information_schema.tables where table_schema = 'public' " +
            "and table_name in ('tenants','users','branches','transactions','audit_logs','attachments'," +
            "'petty_cash_requests','cars','invoices','feedback','release_notes','impersonation_sessions')");
        Assert.Equal(12, tables);
    }

    [Fact]
    public async Task Audit_and_hardening_migrations_are_safe_to_run_twice()
    {
        var db = await _pg.CreateDatabaseAsync();

        // The template already ran them once; running them again must be a no-op, not an error.
        foreach (var name in new[] { "0006_audit_and_impersonation.sql", "0007_tenant_hardening.sql" })
        {
            var file = PostgresFixture.MigrationFiles().Single(f => f.EndsWith(name));
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));
        }
    }

    [Fact]
    public async Task Migration_0006_alone_keeps_the_previous_API_build_working()
    {
        // Rollout order: 0006 (additive) -> deploy the new API -> 0007. Between the first two the
        // old build is still serving, so 0006 must not change anything it depends on.
        var db = await _pg.CreateEmptyDatabaseAsync();
        foreach (var file in PostgresFixture.MigrationFiles().Where(f => string.CompareOrdinal(Path.GetFileName(f), "0007") < 0))
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));

        await using var conn = new NpgsqlConnection(_pg.AppConnectionString(db));
        await conn.OpenAsync();
        await using (var stamp = new NpgsqlCommand("select set_config('app.tenant_id', @t, false), set_config('app.bypass_rls', 'off', false)", conn))
        {
            stamp.Parameters.AddWithValue("t", PostgresFixture.Client1);
            await stamp.ExecuteNonQueryAsync();
        }

        // The old build reads tenants with no bypass scope: still allowed (both rows visible).
        await using (var tenants = new NpgsqlCommand("select count(*) from tenants", conn))
            Assert.Equal(2L, (long)(await tenants.ExecuteScalarAsync())!);

        // And it writes audit rows without the new columns.
        await using (var audit = new NpgsqlCommand($"insert into audit_logs (id, action, tenant_id) values ('a', 'Create', '{PostgresFixture.Client1}')", conn))
            Assert.Equal(1, await audit.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Migration_0007_is_what_locks_the_tenants_table_down()
    {
        var db = await _pg.CreateEmptyDatabaseAsync();
        foreach (var file in PostgresFixture.MigrationFiles())
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));

        await using var conn = new NpgsqlConnection(_pg.AppConnectionString(db));
        await conn.OpenAsync();
        await using (var stamp = new NpgsqlCommand("select set_config('app.tenant_id', @t, false), set_config('app.bypass_rls', 'off', false)", conn))
        {
            stamp.Parameters.AddWithValue("t", PostgresFixture.Client1);
            await stamp.ExecuteNonQueryAsync();
        }

        await using var tenants = new NpgsqlCommand("select count(*) from tenants", conn);
        Assert.Equal(1L, (long)(await tenants.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Existing_data_is_backfilled_to_Client1_and_audit_actor_is_filled()
    {
        // Simulate production: schema and rows exist BEFORE multi-tenancy, then 0004..0006 run.
        var db = await _pg.CreateEmptyDatabaseAsync();
        var files = PostgresFixture.MigrationFiles();
        foreach (var file in files.Where(f => Path.GetFileName(f).StartsWith("0001") || Path.GetFileName(f).StartsWith("0002") || Path.GetFileName(f).StartsWith("0003")))
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));

        await _pg.ExecuteAsync(db, @"
            insert into branches (id, name, code) values ('b1', 'Jakarta', 'JKT');
            insert into users (id, email, role) values ('u1', 'old@admin.test', 'Manager');
            insert into transactions (id, type, amount, transaction_date, branch) values ('t1', 'Income', 10, now(), 'JKT');
            insert into audit_logs (id, user_id, action) values ('a1', 'u1', 'Create');");

        foreach (var file in files.Where(f => !Path.GetFileName(f).StartsWith("0001") && !Path.GetFileName(f).StartsWith("0002") && !Path.GetFileName(f).StartsWith("0003")))
            await _pg.ExecuteAsync(db, await File.ReadAllTextAsync(file));

        foreach (var table in new[] { "branches", "users", "transactions", "audit_logs" })
        {
            var notClient1 = await _pg.ScalarAsync<long>(db,
                $"select count(*) from {table} where tenant_id <> '{PostgresFixture.Client1}'");
            Assert.Equal(0, notClient1);
        }

        Assert.Equal("u1", await _pg.ScalarAsync<string>(db, "select actor_user_id from audit_logs where id = 'a1'"));
        Assert.Null(await _pg.ScalarAsync<string>(db, "select acting_as_user_id from audit_logs where id = 'a1'"));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Inserting_without_a_tenant_fails_instead_of_silently_landing_in_Client1(string table)
    {
        var db = await _pg.CreateDatabaseAsync();

        // Same insert as the seed helper, minus tenant_id: with the default gone this must fail.
        var insert = SeedData.InsertByTable[table]
            .Replace(", tenant_id)", ")")
            .Replace(", @t)", ")");

        await using var conn = new NpgsqlConnection(_pg.AdminConnectionString(db));
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(insert, conn);
        cmd.Parameters.AddWithValue("id", "x1");
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
    }

    [Fact]
    public async Task Branch_codes_are_unique_per_site_not_globally()
    {
        var db = await _pg.CreateDatabaseAsync();
        await _pg.ExecuteAsync(db, $@"
            insert into branches (id, name, code, tenant_id) values ('a', 'Jakarta', 'JKT', '{PostgresFixture.Client1}');
            insert into branches (id, name, code, tenant_id) values ('b', 'Jakarta', 'JKT', '{PostgresFixture.Client2}');");

        var ex = await Assert.ThrowsAsync<PostgresException>(() => _pg.ExecuteAsync(db,
            $"insert into branches (id, name, code, tenant_id) values ('c', 'Jakarta', 'jkt', '{PostgresFixture.Client1}')"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Fact]
    public async Task Email_stays_unique_across_all_sites_because_it_is_the_login()
    {
        var db = await _pg.CreateDatabaseAsync();
        await _pg.ExecuteAsync(db, $"insert into users (id, email, role, tenant_id) values ('u1', 'same@x.test', 'User', '{PostgresFixture.Client1}')");

        var ex = await Assert.ThrowsAsync<PostgresException>(() => _pg.ExecuteAsync(db,
            $"insert into users (id, email, role, tenant_id) values ('u2', 'SAME@x.test', 'User', '{PostgresFixture.Client2}')"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("ImpersonationStart")]
    [InlineData("ImpersonationEnd")]
    [InlineData("TenantCreate")]
    [InlineData("RoleChange")]
    public async Task Audit_accepts_the_new_event_types(string action)
    {
        var db = await _pg.CreateDatabaseAsync();
        await _pg.ExecuteAsync(db, $"insert into audit_logs (id, action, tenant_id, actor_user_id, acting_as_user_id, impersonation_session_id, metadata) " +
                                   $"values ('a', '{action}', '', 'admin', 'target', 'sess', '{{}}')");
    }

    [Fact]
    public async Task Audit_rejects_an_unknown_event_type()
    {
        var db = await _pg.CreateDatabaseAsync();
        var ex = await Assert.ThrowsAsync<PostgresException>(() => _pg.ExecuteAsync(db,
            "insert into audit_logs (id, action, tenant_id) values ('a', 'Hack', '')"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }
}
