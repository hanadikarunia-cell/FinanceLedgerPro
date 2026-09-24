namespace FinanceLedger.Tests.Infrastructure;

/// <summary>
/// Minimal rows for every tenant-scoped table. Only the columns each table requires are set,
/// so the tests stay readable and do not break when optional columns are added.
/// </summary>
public static class SeedData
{
    /// <summary>The 8 tenant-scoped tables, each with an INSERT that takes (@id, @t).</summary>
    public static readonly IReadOnlyDictionary<string, string> InsertByTable = new Dictionary<string, string>
    {
        ["users"] = "insert into users (id, email, role, tenant_id) values (@id, @id || '@test.local', 'User', @t)",
        ["branches"] = "insert into branches (id, name, code, tenant_id) values (@id, 'Branch', @id, @t)",
        ["transactions"] = "insert into transactions (id, type, amount, transaction_date, branch, tenant_id) values (@id, 'Income', 1, now(), 'b', @t)",
        ["audit_logs"] = "insert into audit_logs (id, action, tenant_id) values (@id, 'Create', @t)",
        ["attachments"] = "insert into attachments (id, tenant_id) values (@id, @t)",
        ["petty_cash_requests"] = "insert into petty_cash_requests (id, amount, branch, status, tenant_id) values (@id, 1, 'b', 'Draft', @t)",
        ["cars"] = "insert into cars (id, branch, contract_start_date, tenant_id) values (@id, 'b', now(), @t)",
        ["invoices"] = "insert into invoices (id, type, branch, tenant_id) values (@id, 'Rental', 'b', @t)",
    };

    public static IEnumerable<object[]> Tables() => InsertByTable.Keys.Select(t => new object[] { t });
}
