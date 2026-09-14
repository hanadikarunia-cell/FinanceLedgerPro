using FinanceLedger.Application.Interfaces;

namespace FinanceLedger.Application.Services;

public class SyncService : ISyncService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IGoogleSheetsService _googleSheetsService;

    public SyncService(ITransactionRepository transactionRepository, IGoogleSheetsService googleSheetsService)
    {
        _transactionRepository = transactionRepository;
        _googleSheetsService = googleSheetsService;
    }

    public async Task<int> SyncApprovedTransactionsAsync(CancellationToken ct = default)
    {
        var pending = await _transactionRepository.GetApprovedNotSyncedAsync(ct);
        if (pending.Count == 0)
        {
            return 0;
        }

        await _googleSheetsService.SyncTransactionsAsync(pending, ct);
        return pending.Count;
    }
}
