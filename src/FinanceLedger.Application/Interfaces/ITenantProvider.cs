namespace FinanceLedger.Application.Interfaces;

/// <summary>
/// Supplies the site (tenant) the current operation is scoped to. The DbContext
/// reads this on every query and every save, so all data access is confined to it.
/// Null means "no site" (an Application Admin outside any site, or an unauthenticated
/// call): tenant-scoped queries then match nothing and inserts of tenant-scoped rows
/// must name their tenant explicitly.
/// </summary>
public interface ITenantProvider
{
    string? TenantId { get; }
}
