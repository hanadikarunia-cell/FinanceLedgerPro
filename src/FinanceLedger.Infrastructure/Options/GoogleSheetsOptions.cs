namespace FinanceLedger.Infrastructure.Options;

public class GoogleSheetsOptions
{
    public string SpreadsheetId { get; set; } = string.Empty;
    public string SheetName { get; set; } = "Transactions";
    public string CredentialsJson { get; set; } = string.Empty;
}
