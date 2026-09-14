using System.Globalization;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Infrastructure.Options;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;

namespace FinanceLedger.Infrastructure.Integration;

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly GoogleSheetsOptions _options;

    public GoogleSheetsService(IOptions<GoogleSheetsOptions> options)
    {
        _options = options.Value;
    }

    public async Task SyncTransactionsAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default)
    {
        // No-op when the integration has not been configured. This keeps the
        // background sync path harmless in environments without credentials.
        if (string.IsNullOrWhiteSpace(_options.SpreadsheetId) ||
            string.IsNullOrWhiteSpace(_options.CredentialsJson))
        {
            return;
        }

        if (transactions.Count == 0)
            return;

        var credential = GoogleCredential
            .FromJson(_options.CredentialsJson)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        using var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Finance Ledger Pro"
        });

        var rows = new List<IList<object>>(transactions.Count);
        foreach (var t in transactions.OrderBy(t => t.Date))
        {
            rows.Add(new List<object>
            {
                t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                t.Type.ToString(),
                t.Category,
                t.Description,
                t.Amount.ToString(CultureInfo.InvariantCulture),
                t.Branch,
                t.ApprovalStatus.ToString()
            });
        }

        var valueRange = new ValueRange { Values = rows };
        var range = $"{_options.SheetName}!A:G";

        var request = service.Spreadsheets.Values.Append(valueRange, _options.SpreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync(ct);
    }
}
