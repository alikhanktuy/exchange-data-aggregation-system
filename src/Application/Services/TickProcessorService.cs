using System.Diagnostics;
using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <summary>Deduplicates ticks and persists them using size- or time-triggered batches.</summary>
public sealed class TickProcessorService : ITickProcessor, ITickProcessorMetrics
{
    private readonly IDeduplicationService _deduplication;
    private readonly ITickRepository _repository;
    private readonly ILogger<TickProcessorService> _logger;
    private readonly PipelineOptions _options;
    private long _totalReceived;
    private long _totalDeduplicated;
    private long _totalInserted;
    private long _totalErrors;
    private long _batchCount;

    /// <summary>Creates a tick processor.</summary>
    public TickProcessorService(
        IDeduplicationService deduplication,
        ITickRepository repository,
        IOptions<PipelineOptions> options,
        ILogger<TickProcessorService> logger)
    {
        _deduplication = deduplication;
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(ChannelReader<Tick> reader, CancellationToken ct)
    {
        var batch = new List<Tick>(_options.BatchSize);
        var stopwatch = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var readTask = reader.WaitToReadAsync(ct).AsTask();
                var delayTask = Task.Delay(_options.FlushIntervalMs, ct);
                var completed = await Task.WhenAny(readTask, delayTask);

                if (completed == readTask && !await readTask)
                {
                    await FlushAsync(batch, ct);
                    return;
                }

                while (reader.TryRead(out var tick))
                {
                    Interlocked.Increment(ref _totalReceived);
                    if (await _deduplication.IsDuplicateAsync(tick, ct))
                    {
                        Interlocked.Increment(ref _totalDeduplicated);
                        continue;
                    }

                    batch.Add(tick);
                    if (batch.Count >= _options.BatchSize)
                    {
                        await FlushAsync(batch, ct);
                        stopwatch.Restart();
                    }
                }

                if (batch.Count > 0 && stopwatch.ElapsedMilliseconds >= _options.FlushIntervalMs)
                {
                    await FlushAsync(batch, ct);
                    stopwatch.Restart();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalErrors);
                _logger.LogError(ex, "Tick processor error");
            }
        }

        await FlushAsync(batch, CancellationToken.None);
    }

    /// <inheritdoc />
    public TickProcessorMetrics Snapshot() => new(
        Interlocked.Read(ref _totalReceived),
        Interlocked.Read(ref _totalDeduplicated),
        Interlocked.Read(ref _totalInserted),
        Interlocked.Read(ref _totalErrors),
        Interlocked.Read(ref _batchCount));

    private async Task FlushAsync(List<Tick> batch, CancellationToken ct)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var count = batch.Count;
        await _repository.InsertBatchAsync(batch, ct);
        Interlocked.Add(ref _totalInserted, count);
        Interlocked.Increment(ref _batchCount);
        batch.Clear();
    }
}
