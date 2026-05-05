namespace Application.Services;

/// <summary>Thread-safe snapshot of tick processing counters.</summary>
public sealed record TickProcessorMetrics(
    long TotalReceived,
    long TotalDeduplicated,
    long TotalInserted,
    long TotalErrors,
    long BatchCount);
