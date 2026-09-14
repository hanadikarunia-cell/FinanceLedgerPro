namespace FinanceLedger.Domain.Enums;

/// <summary>
/// Which amounts PPN/PPH23 are calculated against on a Service Bill invoice — negotiated
/// per client deal. PPN is always 11%; PPH23 is always 2%.
/// </summary>
public enum TaxScheme
{
    /// <summary>PPN on (Wage Deposit + Fee); PPH23 on Fee only.</summary>
    Combined = 1,

    /// <summary>PPN and PPH23 both on Wage Deposit only.</summary>
    WageOnly = 2,

    /// <summary>PPN and PPH23 both on Fee only.</summary>
    FeeOnly = 3
}
