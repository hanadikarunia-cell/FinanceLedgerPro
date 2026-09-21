namespace FinanceLedger.Domain.Common;

/// <summary>
/// An aggregate that belongs to exactly one site (tenant). The DbContext stamps
/// TenantId on insert and filters every query by the current tenant, so one
/// site can never read or modify another site's rows.
/// </summary>
public interface ITenantEntity : IEntity
{
    string TenantId { get; set; }
}
