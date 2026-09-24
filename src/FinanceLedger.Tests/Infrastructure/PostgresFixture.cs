using System.Net;
using System.Net.Sockets;
using MysticMind.PostgresEmbed;
using Npgsql;
using Xunit;

namespace FinanceLedger.Tests.Infrastructure;

/// <summary>
/// One real Postgres server for the whole test run (downloaded once by MysticMind.PostgresEmbed,
/// no Docker, no account, nothing shared with production). The real supabase/migrations/*.sql
/// files are applied to a template database once; every test class then clones it, so tests
/// run against exactly the schema and row-level-security policies that production has.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string AppApiPassword = "app_api_test_password";

    // Fixed ids seeded by migration 0004.
    public const string Client1 = "00000000-0000-0000-0000-000000000001";
    public const string Client2 = "00000000-0000-0000-0000-000000000002";

    // The embedded server's stderr is a pipe nobody drains after startup, and Postgres blocks when
    // writing to a full pipe. These tests provoke many errors on purpose, so logging them would
    // eventually hang the next statement. Errors still reach the client; they are just not logged.
    private static readonly Dictionary<string, string> QuietServerParams = new()
    {
        ["log_min_messages"] = "panic",
        ["log_min_error_statement"] = "panic",
        ["log_connections"] = "off",
        ["log_disconnections"] = "off",
        ["log_statement"] = "none",
    };

    private PgServer? _server;
    private int _port;
    private int _dbCounter;

    private const string TemplateDb = "ledger_template";

    public async Task InitializeAsync()
    {
        _port = FreePort();
        _server = new PgServer(
            "15.3.0",
            port: _port,
            addLocalUserAccessPermission: true,
            clearWorkingDirOnStart: false,
            pgServerParams: QuietServerParams);
        await _server.StartAsync();

        await ExecuteAsync("postgres", "create database " + TemplateDb);
        foreach (var file in MigrationFiles())
            await ExecuteAsync(TemplateDb, await File.ReadAllTextAsync(file));

        // The migration creates app_api with no password (so it is safe to commit); tests set one.
        await ExecuteAsync("postgres", $"alter role app_api with login password '{AppApiPassword}'");
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        if (_server is not null)
            await _server.StopAsync();
    }

    /// <summary>All migrations, oldest first.</summary>
    public static IReadOnlyList<string> MigrationFiles() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "migrations"), "*.sql")
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();

    /// <summary>A fresh, fully migrated database (cloned from the template).</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"t{Interlocked.Increment(ref _dbCounter)}_{Guid.NewGuid():N}"[..30];
        await ExecuteAsync("postgres", $"create database {name} template {TemplateDb}");
        return name;
    }

    /// <summary>An empty database, for tests that apply migrations themselves step by step.</summary>
    public async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = $"e{Interlocked.Increment(ref _dbCounter)}_{Guid.NewGuid():N}"[..30];
        await ExecuteAsync("postgres", "create database " + name);
        return name;
    }

    /// <summary>Superuser connection string: seeds data and reads it back, bypassing RLS.</summary>
    public string AdminConnectionString(string db) =>
        $"Host=localhost;Port={_port};Database={db};Username=postgres;Pooling=false";

    /// <summary>The restricted role production uses. RLS applies to it.</summary>
    public string AppConnectionString(string db, bool pooling = false) =>
        $"Host=localhost;Port={_port};Database={db};Username=app_api;Password={AppApiPassword};Pooling={pooling}";

    public async Task ExecuteAsync(string db, string sql)
    {
        await using var conn = new NpgsqlConnection(AdminConnectionString(db));
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string db, string sql)
    {
        await using var conn = new NpgsqlConnection(AdminConnectionString(db));
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        var value = await cmd.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)Convert.ChangeType(value, typeof(T));
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
