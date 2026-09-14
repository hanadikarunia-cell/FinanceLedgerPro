using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Interfaces;

public interface IGoogleSheetsService
{
    Task SyncTransactionsAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default);
}
