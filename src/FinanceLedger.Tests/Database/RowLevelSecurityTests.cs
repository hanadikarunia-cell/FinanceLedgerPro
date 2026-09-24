using FinanceLedger.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace FinanceLedger.Tests.Database;

/// <summary>
/// Row-level security, exercised directly as the restricted app_api role - independent of the
/// .NET application - so a bug in the app's own filtering could never hide a database-level gap.
/// Covers SELECT, INSERT, UPDATE and DELETE across sites for every tenant-scoped table.
/// </summary>
[Collection(PostgresCollection.Name)]
public class RowLevelSecurityTests
{
    private const string A = PostgresFixture.Client1;
    private const string B = PostgresFixture.Client2;

    private readonly PostgresFixture _pg;

    public RowLevelSecurityTests(PostgresFixture pg) => _pg = pg;

    // ---- helpers ---------------------------------------------------------------------

    private async Task<string> SeedTwoSitesAsync(string table)
    {
        var db = await _pg.CreateDatabaseAsync();
        await InsertAsAdminAsync(db, table, "a1", A);
        await InsertAsAdminAsync(db, table, "b1", B);
        return db;
    }

    private async Task InsertAsAdminAsync(string db, string table, string id, string tenant)
    {
        await using var conn = new NpgsqlConnection(_pg.AdminConnectionString(db));
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(SeedData.InsertByTable[table], conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("t", tenant);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>A connection as app_api, stamped exactly the way TenantSessionInterceptor does it.</summary>
    private async Task<NpgsqlConnection> AppSessionAsync(string db, string? tenant, bool bypass = false)
    {
        var conn = new NpgsqlConnection(_pg.AppConnectionString(db));
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "select set_config('app.tenant_id', @t, false), set_config('app.bypass_rls', @b, false)", conn);
        cmd.Parameters.AddWithValue("t", tenant ?? string.Empty);
        cmd.Parameters.AddWithValue("b", bypass ? "on" : "off");
        await cmd.ExecuteNonQueryAsync();
        return conn;
    }

    private static async Task<List<string>> IdsAsync(NpgsqlConnection conn, string table)
    {
        var ids = new List<string>();
        await using var cmd = new NpgsqlCommand($"select id from {table} order by id", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task<int> ExecAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        return await cmd.ExecuteNonQueryAsync();
    }

    // ---- SELECT ----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_reads_only_its_own_rows(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, A);
        Assert.Equal(new[] { "a1" }, await IdsAsync(conn, table));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_B_reads_only_its_own_rows(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, B);
        Assert.Equal(new[] { "b1" }, await IdsAsync(conn, table));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task A_session_with_no_site_and_no_bypass_sees_nothing(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, null);
        Assert.Empty(await IdsAsync(conn, table));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task A_session_that_never_had_a_site_stamped_sees_nothing(string table)
    {
        // Not even set_config: a connection nobody stamped must fail closed, not open.
        var db = await SeedTwoSitesAsync(table);
        await using var conn = new NpgsqlConnection(_pg.AppConnectionString(db));
        await conn.OpenAsync();
        Assert.Empty(await IdsAsync(conn, table));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Bypass_sees_every_site(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, null, bypass: true);
        Assert.Equal(new[] { "a1", "b1" }, await IdsAsync(conn, table));
    }

    // ---- INSERT ----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_can_insert_into_its_own_site(string table)
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, A);
        await using var cmd = new NpgsqlCommand(SeedData.InsertByTable[table], conn);
        cmd.Parameters.AddWithValue("id", "n1");
        cmd.Parameters.AddWithValue("t", A);
        Assert.Equal(1, await cmd.ExecuteNonQueryAsync());
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_cannot_insert_a_row_into_site_B(string table)
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, A);
        await using var cmd = new NpgsqlCommand(SeedData.InsertByTable[table], conn);
        cmd.Parameters.AddWithValue("id", "evil");
        cmd.Parameters.AddWithValue("t", B);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
        Assert.Equal(0L, await _pg.ScalarAsync<long>(db, $"select count(*) from {table} where id = 'evil'"));
    }

    // ---- UPDATE / DELETE -------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_updates_its_own_row_but_not_site_Bs(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, A);

        Assert.Equal(1, await ExecAsync(conn, $"update {table} set id = id where id = 'a1'"));
        Assert.Equal(0, await ExecAsync(conn, $"update {table} set id = id where id = 'b1'"));
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_cannot_hand_its_row_to_site_B(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, A);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecAsync(conn, $"update {table} set tenant_id = '{B}' where id = 'a1'"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
    }

    [Theory]
    [MemberData(nameof(SeedData.Tables), MemberType = typeof(SeedData))]
    public async Task Site_A_cannot_delete_site_Bs_row(string table)
    {
        var db = await SeedTwoSitesAsync(table);
        await using var conn = await AppSessionAsync(db, A);

        Assert.Equal(0, await ExecAsync(conn, $"delete from {table} where id = 'b1'"));
        Assert.Equal(1L, await _pg.ScalarAsync<long>(db, $"select count(*) from {table} where id = 'b1'"));
        Assert.Equal(1, await ExecAsync(conn, $"delete from {table} where id = 'a1'"));
    }

    // ---- tenants ---------------------------------------------------------------------

    [Fact]
    public async Task A_site_can_read_only_its_own_tenant_row()
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, A);
        Assert.Equal(new[] { A }, await IdsAsync(conn, "tenants"));
    }

    [Fact]
    public async Task A_session_with_no_site_and_no_bypass_sees_no_tenants()
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, null);
        Assert.Empty(await IdsAsync(conn, "tenants"));
    }

    [Fact]
    public async Task Bypass_sees_all_tenants_and_can_manage_them()
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, null, bypass: true);

        Assert.Equal(new[] { A, B }, await IdsAsync(conn, "tenants"));
        Assert.Equal(1, await ExecAsync(conn, "insert into tenants (id, name, code) values ('t3', 'Client 3', 'C3')"));
        Assert.Equal(1, await ExecAsync(conn, "update tenants set name = 'Renamed' where id = 't3'"));
        Assert.Equal(1, await ExecAsync(conn, "delete from tenants where id = 't3'"));
    }

    [Fact]
    public async Task A_site_can_never_write_to_tenants_not_even_its_own_row()
    {
        var db = await _pg.CreateDatabaseAsync();
        await using var conn = await AppSessionAsync(db, A);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecAsync(conn, "insert into tenants (id, name, code) values ('t3', 'Sneaky', 'SNEAK')"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);

        Assert.Equal(0, await ExecAsync(conn, $"update tenants set is_active = false where id = '{A}'"));
        Assert.Equal(0, await ExecAsync(conn, $"update tenants set is_active = false where id = '{B}'"));
        Assert.Equal(0, await ExecAsync(conn, $"delete from tenants where id = '{B}'"));
        Assert.Equal(2L, await _pg.ScalarAsync<long>(db, "select count(*) from tenants where is_active"));
    }

    // ---- impersonation_sessions ------------------------------------------------------

    private async Task<string> SeedImpersonationAsync()
    {
        var db = await _pg.CreateDatabaseAsync();
        await _pg.ExecuteAsync(db, $@"
            insert into users (id, email, role, tenant_id) values ('admin', 'admin@x.test', 'AppAdmin', '');
            insert into users (id, email, role, tenant_id) values ('target', 'target@x.test', 'User', '{B}');
            insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, expires_at)
              values ('s1', 'admin', 'target', '{B}', now() + interval '1 hour');");
        return db;
    }

    [Fact]
    public async Task Impersonation_sessions_are_invisible_without_bypass()
    {
        var db = await SeedImpersonationAsync();

        await using var asSite = await AppSessionAsync(db, B);
        Assert.Empty(await IdsAsync(asSite, "impersonation_sessions"));

        await using var asAppAdminOutsideBypass = await AppSessionAsync(db, "");
        Assert.Empty(await IdsAsync(asAppAdminOutsideBypass, "impersonation_sessions"));

        await using var bypass = await AppSessionAsync(db, null, bypass: true);
        Assert.Equal(new[] { "s1" }, await IdsAsync(bypass, "impersonation_sessions"));
    }

    [Fact]
    public async Task A_site_cannot_create_forge_or_end_an_impersonation_session()
    {
        var db = await SeedImpersonationAsync();
        await using var conn = await AppSessionAsync(db, B);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(conn,
            $"insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, expires_at) " +
            $"values ('s2', 'admin', 'target', '{B}', now() + interval '1 hour')"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);

        Assert.Equal(0, await ExecAsync(conn, "update impersonation_sessions set ended_at = now() where id = 's1'"));
        Assert.Equal(0, await ExecAsync(conn, "delete from impersonation_sessions where id = 's1'"));
    }

    [Fact]
    public async Task Impersonation_session_must_expire_after_it_starts()
    {
        var db = await SeedImpersonationAsync();
        await using var conn = await AppSessionAsync(db, null, bypass: true);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(conn,
            $"insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, started_at, expires_at) " +
            $"values ('s3', 'admin', 'target', '{B}', now(), now() - interval '1 minute')"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

    [Fact]
    public async Task Impersonation_session_must_point_at_a_real_user_and_site()
    {
        var db = await SeedImpersonationAsync();
        await using var conn = await AppSessionAsync(db, null, bypass: true);

        var noUser = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(conn,
            $"insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, expires_at) " +
            $"values ('s4', 'admin', 'nobody', '{B}', now() + interval '1 hour')"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, noUser.SqlState);

        var noSite = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(conn,
            "insert into impersonation_sessions (id, admin_user_id, target_user_id, target_tenant_id, expires_at) " +
            "values ('s5', 'admin', 'target', 'no-such-site', now() + interval '1 hour')"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, noSite.SqlState);
    }
}
