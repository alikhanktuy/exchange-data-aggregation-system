using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Application.Services;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

BenchmarkRunner.Run<TickPipelineBenchmarks>();

/// <summary>Benchmarks the in-memory tick processing pipeline without external infrastructure.</summary>
[MemoryDiagnoser]
public class TickPipelineBenchmarks
{
    private Tick[] _ticks = [];

    /// <summary>Number of ticks submitted to the processor.</summary>
    [Params(100, 1_000, 10_000)]
    public int TickCount { get; set; }

    /// <summary>Creates deterministic benchmark data.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var start = DateTimeOffset.UtcNow;
        _ticks = Enumerable.Range(0, TickCount)
            .Select(i => new Tick(
                i % 2 == 0 ? "BTCUSDT" : "ETHUSDT",
                60_000m + i,
                0.1m + i % 10,
                start.AddMilliseconds(i),
                i % 3 == 0 ? "ExchangeA" : i % 3 == 1 ? "ExchangeB" : "ExchangeC"))
            .ToArray();
    }

    /// <summary>Processes unique ticks through deduplication and batched repository insert.</summary>
    [Benchmark]
    public async Task ProcessUniqueTicks()
    {
        var repository = new BenchmarkRepository();
        var processor = new TickProcessorService(
            new BenchmarkDeduplicationService(),
            repository,
            Options.Create(new PipelineOptions
            {
                BatchSize = 200,
                FlushIntervalMs = 500,
                ChannelCapacity = TickCount + 1
            }),
            NullLogger<TickProcessorService>.Instance);
        var channel = Channel.CreateBounded<Tick>(new BoundedChannelOptions(TickCount + 1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var processing = processor.ProcessAsync(channel.Reader, CancellationToken.None);
        foreach (var tick in _ticks)
        {
            await channel.Writer.WriteAsync(tick);
        }

        channel.Writer.Complete();
        await processing;
    }

    private sealed class BenchmarkRepository : ITickRepository
    {
        public Task InsertBatchAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class BenchmarkDeduplicationService : IDeduplicationService
    {
        private readonly ConcurrentDictionary<string, byte> _keys = new();

        public Task<bool> IsDuplicateAsync(Tick tick, CancellationToken ct = default)
        {
            var key = $"{tick.Source}:{tick.Ticker}:{tick.Timestamp.ToUnixTimeMilliseconds()}";
            return Task.FromResult(!_keys.TryAdd(key, 0));
        }
    }
}
