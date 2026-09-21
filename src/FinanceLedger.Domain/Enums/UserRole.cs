namespace FinanceLedger.Domain.Enums;

/// <summary>
/// Manager is the per-site administrator ("Site Admin" in the UI). AppAdmin is the
/// application-level administrator who manages sites and belongs to no site.
/// Values are persisted as strings, so the order here is not significant.
/// </summary>
public enum UserRole
{
    Manager,
    User,
    AppAdmin
}
