using FinanceLedger.Domain.Common;

namespace FinanceLedger.Domain.Entities;

/// <summary>A client site. Not itself tenant-scoped — it is the thing that scopes everything else.</summary>
public class Tenant : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
