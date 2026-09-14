using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Domain.Constants;

public static class TransactionCategories
{
    /// <summary>Internal-only category used for the auto-created petty cash top-up
    /// transaction. Never shown as a selectable option in either category list.</summary>
    public const string PettyCash = "Petty Cash";

    public static readonly IReadOnlyList<string> IncomeCategories = new[]
    {
        "Rent", "Interest", "Invoice", "Salaries", "Other"
    };

    public static readonly IReadOnlyList<string> ExpenseCategories = new[]
    {
        "Service", "Salaries", "Entertainment", "Office Utilities",
        "Taxes - PPN", "Taxes - PPH21", "Taxes - PPH25", "Taxes - PPH23", "Taxes - Other",
        "Car Debt", "Other"
    };

    public static readonly IReadOnlySet<string> RequiresApproval =
        new HashSet<string>(new[] { "Entertainment" }, StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(TransactionType type, string category)
    {
        if (string.Equals(category, PettyCash, StringComparison.OrdinalIgnoreCase))
            return true;

        var list = type == TransactionType.Income ? IncomeCategories : ExpenseCategories;
        return list.Contains(category, StringComparer.OrdinalIgnoreCase);
    }
}
