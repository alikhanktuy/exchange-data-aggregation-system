namespace Application.Configuration;

/// <summary>Configuration for the ingestion pipeline.</summary>
public sealed class PipelineOptions
{
    /// <summary>Number of ticks that triggers a database flush.</summary>
    public int BatchSize { get; init; } = 200;

    /// <summary>Maximum flush interval in milliseconds.</summary>
    public int FlushIntervalMs { get; init; } = 500;

    /// <summary>Redis deduplication time window in seconds.</summary>
    public int DeduplicationWindowSec { get; init; } = 5;

    /// <summary>Bounded channel capacity.</summary>
    public int ChannelCapacity { get; init; } = 10_000;

    /// <summary>Maximum reconnect attempts before a source stops.</summary>
    public int ReconnectMaxRetries { get; init; } = 5;
}
