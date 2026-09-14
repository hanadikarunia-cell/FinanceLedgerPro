using FinanceLedger.Application.Interfaces;
using FinanceLedger.Worker.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceLedger.Worker;

/// <summary>
/// Background service that periodically syncs approved-but-not-yet-synced
/// transactions to Google Sheets. In addition to the timer, it drains manual
/// trigger requests from <see cref="ManualSyncTrigger"/> for on-demand runs.
/// </summary>
public sealed class GoogleSheetsSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GoogleSheetsSyncWorker> _logger;
    private readonly ManualSyncTrigger _manualTrigger;
    private readonly SyncOptions _options;

    public GoogleSheetsSyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<GoogleSheetsSyncWorker> logger,
        ManualSyncTrigger manualTrigger,
        IOptions<SyncOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _manualTrigger = manualTrigger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Google Sheets sync is disabled via configuration. Worker idle.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        _logger.LogInformation("Google Sheets sync worker started. Interval: {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);

        // Kick off a background loop that reacts to manual triggers.
        var manualLoop = DrainManualTriggersAsync(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await SafeSyncAsync("timer", stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }

        await manualLoop.ConfigureAwait(false);
        _logger.LogInformation("Google Sheets sync worker stopped.");
    }

    private async Task DrainManualTriggersAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _manualTrigger.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_manualTrigger.Reader.TryRead(out _))
                {
                    await SafeSyncAsync("manual", stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
    }

    private async Task SafeSyncAsync(string source, CancellationToken ct)
    {
        try
        {
            var synced = await DoSyncAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Sync ({Source}) completed. {Count} transaction(s) synced.", source, synced);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Swallow so the loop keeps running until the host is stopped.
            _logger.LogError(ex, "Sync ({Source}) failed. Worker will continue on the next tick.", source);
        }
    }

    /// <summary>
    /// Performs a single sync cycle and returns the number of transactions synced.
    /// This is the "manual trigger" entry point and can be wired to an admin
    /// HTTP endpoint, a service-bus/queue message handler, or a diagnostics tool.
    /// Unlike the timer path it propagates exceptions to the caller so failures
    /// can be surfaced to the operator.
    /// </summary>
    public Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
        => DoSyncAsync(cancellationToken);

    private async Task<int> DoSyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
        return await sync.SyncApprovedTransactionsAsync(ct).ConfigureAwait(false);
    }
}
