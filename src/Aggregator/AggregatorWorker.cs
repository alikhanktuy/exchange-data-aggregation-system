using Application.Services;

namespace Aggregator;

/// <summary>BackgroundService entry point that runs aggregation and monitoring loops.</summary>
public sealed class AggregatorWorker : BackgroundService
{
    private readonly TickAggregatorService _aggregator;
    private readonly ITickProcessorMetrics _metrics;
    private readonly ILogger<AggregatorWorker> _logger;

    /// <summary>Creates an aggregator worker.</summary>
    public AggregatorWorker(
        TickAggregatorService aggregator,
        ITickProcessorMetrics metrics,
        ILogger<AggregatorWorker> logger)
    {
        _aggregator = aggregator;
        _metrics = metrics;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var aggregationTask = _aggregator.RunAsync(stoppingToken);
        var monitorTask = MonitorAsync(stoppingToken);
        await Task.WhenAll(aggregationTask, monitorTask);
    }

    private async Task MonitorAsync(CancellationToken ct)
    {
        var previousReceived = 0L;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            var snapshot = _metrics.Snapshot();
            var rate = (snapshot.TotalReceived - previousReceived) / 10.0;
            previousReceived = snapshot.TotalReceived;

            _logger.LogInformation(
                "[Monitor] Received: {Received} | Deduped: {Deduped} | Inserted: {Inserted} | Batches: {Batches} | Errors: {Errors} | Rate: {Rate}/sec",
                snapshot.TotalReceived,
                snapshot.TotalDeduplicated,
                snapshot.TotalInserted,
                snapshot.BatchCount,
                snapshot.TotalErrors,
                rate);
        }
    }
}
