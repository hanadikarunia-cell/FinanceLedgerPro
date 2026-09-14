using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IExcelExportService
{
    FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request);
}

public interface IPdfExportService
{
    FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request);
}

public interface ICsvExportService
{
    FileResultDto Export(IReadOnlyList<Transaction> transactions, ExportRequest request);
}
