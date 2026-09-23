namespace FinanceLedger.Application.Common;

/// <summary>
/// Ambient (per-async-flow) flag read by Infrastructure's TenantSessionInterceptor to tell
/// the database session to bypass row-level security for the current logical connection
/// open. Only the app's own few cross-site code paths open a scope: the *AnyTenant
/// repository methods, site creation/management, and startup seeding. Request input can
/// never reach this - there is no header or parameter that sets it.
/// </summary>
public static class TenantBypassContext
{
    private static readonly AsyncLocal<bool> Flag = new();

    public static bool IsBypassing => Flag.Value;

    public static IDisposable Begin()
    {
        var previous = Flag.Value;
        Flag.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly bool _previous;
        private bool _disposed;

        public Scope(bool previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Flag.Value = _previous;
        }
    }
}
