using System.Globalization;
using System.Text;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Infrastructure.Export;

public class CsvExportService : ICsvExportService
{
    public FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request)
    {
        var builder = new StringBuilder();

        var headers = new[] { "Date", "Type", "Category", "Description", "Amount", "Branch", "Status", "CreatedBy" };
        builder.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (var t in transactions.OrderBy(t => t.Date))
        {
            var fields = new[]
            {
                t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                t.Type.ToString(),
                t.Category,
                t.Description,
                t.Amount.ToString(CultureInfo.InvariantCulture),
                t.Branch,
                t.ApprovalStatus.ToString(),
                t.CreatedByName
            };

            builder.AppendLine(string.Join(",", fields.Select(Escape)));
        }

        var content = Encoding.UTF8.GetBytes(builder.ToString());

        return new FileResultDto
        {
            Content = content,
            FileName = $"Financial_Report_{DateTime.UtcNow:yyyyMMdd}.csv",
            ContentType = "text/csv"
        };
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
