namespace FinanceLedger.Application.DTOs;

public class TenantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int UserCount { get; set; }
}

/// <summary>Creates a site together with its first Site Admin, so the site is usable immediately.</summary>
public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminDisplayName { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}

public class UpdateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
