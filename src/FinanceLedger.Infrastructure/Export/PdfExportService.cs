using System.Globalization;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinanceLedger.Infrastructure.Export;

public class PdfExportService : IPdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request)
    {
        var companyName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? "Finance Ledger Pro"
            : request.CompanyName!;

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? "Financial Report"
            : request.Title!;

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var net = totalIncome - totalExpense;

        var period = BuildPeriod(request);
        var ordered = transactions.OrderBy(t => t.Date).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text(companyName).FontSize(18).Bold();
                    header.Item().Text(title).FontSize(13).SemiBold();
                    if (!string.IsNullOrEmpty(period))
                        header.Item().Text(period).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Spacing(12);

                    content.Item().Background(Colors.Grey.Lighten3).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Total Income: {Money(totalIncome)}");
                        row.RelativeItem().Text($"Total Expense: {Money(totalExpense)}");
                        row.RelativeItem().Text($"Net: {Money(net)}").Bold();
                        row.RelativeItem().Text($"Transactions: {transactions.Count}");
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h.Cell(), "Date");
                            HeaderCell(h.Cell(), "Type");
                            HeaderCell(h.Cell(), "Category");
                            HeaderCell(h.Cell(), "Description");
                            HeaderCell(h.Cell(), "Amount");
                            HeaderCell(h.Cell(), "Branch");
                            HeaderCell(h.Cell(), "Status");
                        });

                        foreach (var t in ordered)
                        {
                            BodyCell(table.Cell(), t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                            BodyCell(table.Cell(), t.Type.ToString());
                            BodyCell(table.Cell(), t.Category);
                            BodyCell(table.Cell(), t.Description);
                            BodyCell(table.Cell(), Money(t.Amount));
                            BodyCell(table.Cell(), t.Branch);
                            BodyCell(table.Cell(), t.ApprovalStatus.ToString());
                        }
                    });

                    content.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().PaddingTop(20).LineHorizontal(1);
                            col.Item().Text("Prepared by");
                        });

                        row.ConstantItem(60);

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().PaddingTop(20).LineHorizontal(1);
                            col.Item().Text("Approved by");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ");
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                    text.Span("  -  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        var bytes = document.GeneratePdf();

        return new FileResultDto
        {
            Content = bytes,
            FileName = $"Financial_Report_{DateTime.UtcNow:yyyyMMdd}.pdf",
            ContentType = "application/pdf"
        };
    }

    private static void HeaderCell(IContainer cell, string text)
    {
        cell.Background(Colors.Grey.Lighten2)
            .Padding(4)
            .Text(text).Bold();
    }

    private static void BodyCell(IContainer cell, string text)
    {
        cell.BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(4)
            .Text(text);
    }

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture);

    private static string BuildPeriod(ExportRequest request)
    {
        if (request.From.HasValue && request.To.HasValue)
            return $"Period: {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}";
        if (request.From.HasValue)
            return $"From: {request.From:yyyy-MM-dd}";
        if (request.To.HasValue)
            return $"To: {request.To:yyyy-MM-dd}";
        return string.Empty;
    }
}
