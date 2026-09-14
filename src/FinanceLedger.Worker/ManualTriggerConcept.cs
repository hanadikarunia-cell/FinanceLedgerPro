using System.Threading.Channels;

namespace FinanceLedger.Worker;

/// <summary>
/// Manual-trigger concept for the sync worker.
///
/// The <see cref="GoogleSheetsSyncWorker"/> normally syncs on a fixed interval.
/// In addition, an operator may want to force an immediate sync (for example
/// from an admin HTTP endpoint, a queue/service-bus message, or a health-tool
/// button) without waiting for the next timer tick.
///
/// This class exposes a bounded single-item <see cref="Channel{T}"/> that acts
/// as a coalescing signal: callers invoke <see cref="TriggerAsync"/> to request
/// a run, and the worker drains <see cref="Reader"/> alongside its
/// <c>PeriodicTimer</c>. Because the channel drops writes when a request is
/// already pending, bursts of triggers collapse into a single extra sync.
///
/// Register it as a singleton so both the producer (endpoint/handler) and the
/// consumer (worker) share the same instance.
/// </summary>
public sealed class ManualSyncTrigger
{
    private readonly Channel<DateTimeOffset> _channel =
        Channel.CreateBounded<DateTimeOffset>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    /// <summary>
    /// Reader the worker awaits to receive manual sync requests.
    /// </summary>
    public ChannelReader<DateTimeOffset> Reader => _channel.Reader;

    /// <summary>
    /// Requests an immediate sync. If a request is already pending it is
    /// coalesced (the call still reports success). Safe to call concurrently.
    /// </summary>
    /// <returns><c>true</c> if a new request was enqueued or one was already pending.</returns>
    public ValueTask<bool> TriggerAsync(CancellationToken ct = default)
    {
        _ = ct;
        // DropWrite mode never blocks; a false result simply means a request
        // was already queued, which is functionally equivalent for the caller.
        _channel.Writer.TryWrite(DateTimeOffset.UtcNow);
        return ValueTask.FromResult(true);
    }
}
