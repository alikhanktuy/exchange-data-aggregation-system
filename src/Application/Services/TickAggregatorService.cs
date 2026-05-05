using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <summary>Starts tick sources and forwards their output through a shared bounded channel.</summary>
public sealed class TickAggregatorService
{
    private readonly IReadOnlyCollection<ITickSource> _sources;
    private readonly ITickProcessor _processor;
    private readonly ILogger<TickAggregatorService> _logger;
    private readonly PipelineOptions _options;

    /// <summary>Creates an aggregator service.</summary>
    public TickAggregatorService(
        IEnumerable<ITickSource> sources,
        ITickProcessor processor,
        IOptions<PipelineOptions> options,
        ILogger<TickAggregatorService> logger)
    {
        _sources = sources.ToArray();
        _processor = processor;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>Runs all sources and the processor until cancellation.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        var channel = Channel.CreateBounded<Tick>(new BoundedChannelOptions(_options.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _logger.LogWarning("Bounded channel uses DropOldest backpressure policy with capacity {Capacity}", _options.ChannelCapacity);

        var processorTask = _processor.ProcessAsync(channel.Reader, ct);
        var sourceTasks = _sources.Select(source => source.StartAsync(channel.Writer, ct)).ToArray();

        try
        {
            await Task.WhenAll(sourceTasks);
        }
        finally
        {
            channel.Writer.TryComplete();
            await processorTask;
        }
    }
}
