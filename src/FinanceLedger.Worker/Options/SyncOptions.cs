namespace FinanceLedger.Worker.Options;

public class SyncOptions
{
    public const string SectionName = "Sync";

    public int IntervalMinutes { get; set; } = 15;

    public bool Enabled { get; set; } = true;
}
