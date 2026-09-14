using ClosedXML.Excel;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Infrastructure.Export;

public class ExcelExportService : IExcelExportService
{
    private const string ContentTypeXlsx =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request)
    {
        using var workbook = new XLWorkbook();

        BuildTransactionsSheet(workbook, transactions);
        BuildCategorySummarySheet(workbook, "Income Summary", transactions, TransactionType.Income);
        BuildCategorySummarySheet(workbook, "Expense Summary", transactions, TransactionType.Expense);
        BuildDashboardSheet(workbook, transactions);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new FileResultDto
        {
            Content = stream.ToArray(),
            FileName = $"Financial_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            ContentType = ContentTypeXlsx
        };
    }

    private static void BuildTransactionsSheet(XLWorkbook workbook, IReadOnlyList<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Transactions");

        var headers = new[] { "Date", "Type", "Category", "Description", "Amount", "Branch", "Status", "CreatedBy" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var t in transactions.OrderBy(t => t.Date))
        {
            sheet.Cell(row, 1).Value = t.Date;
            sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-MM-dd";
            sheet.Cell(row, 2).Value = t.Type.ToString();
            sheet.Cell(row, 3).Value = t.Category;
            sheet.Cell(row, 4).Value = t.Description;
            sheet.Cell(row, 5).Value = t.Amount;
            sheet.Cell(row, 6).Value = t.Branch;
            sheet.Cell(row, 7).Value = t.ApprovalStatus.ToString();
            sheet.Cell(row, 8).Value = t.CreatedByName;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildCategorySummarySheet(
        XLWorkbook workbook,
        string sheetName,
        IReadOnlyList<Transaction> transactions,
        TransactionType type)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        sheet.Cell(1, 1).Value = "Category";
        sheet.Cell(1, 2).Value = "Total";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Style.Font.Bold = true;

        var totals = transactions
            .Where(t => t.Type == type)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        var row = 2;
        foreach (var entry in totals)
        {
            sheet.Cell(row, 1).Value = entry.Category;
            sheet.Cell(row, 2).Value = entry.Total;
            row++;
        }

        sheet.Cell(row, 1).Value = "Grand Total";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = totals.Sum(x => x.Total);
        sheet.Cell(row, 2).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();
    }

    private static void BuildDashboardSheet(XLWorkbook workbook, IReadOnlyList<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Dashboard Statistics");

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var rows = new (string Label, object Value)[]
        {
            ("Total Income", totalIncome),
            ("Total Expense", totalExpense),
            ("Net", totalIncome - totalExpense),
            ("Transaction Count", transactions.Count),
            ("Generated Date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"))
        };

        var row = 1;
        foreach (var (label, value) in rows)
        {
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = XLCellValue.FromObject(value);
            row++;
        }

        sheet.Columns().AdjustToContents();
    }
}
