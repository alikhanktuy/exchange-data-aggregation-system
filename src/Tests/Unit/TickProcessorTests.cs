using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Unit;

/// <summary>Unit tests for tick processing pipeline behavior.</summary>
public sealed class TickProcessorTests
{
    /// <summary>Duplicate ticks are skipped and unique ticks are persisted.</summary>
    [Fact]
    public async Task ProcessorSkipsDuplicatesAndPersistsUniqueTicks()
    {
        var duplicateToggle = 0;
        var dedup = new Mock<IDeduplicationService>();
        dedup.Setup(x => x.IsDuplicateAsync(It.IsAny<Tick>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref duplicateToggle) == 2);

        IReadOnlyCollection<Tick>? persisted = null;
        var repository = new Mock<ITickRepository>();
        repository.Setup(x => x.InsertBatchAsync(It.IsAny<IReadOnlyCollection<Tick>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Tick>, CancellationToken>((batch, _) => persisted = batch.ToArray())
            .Returns(Task.CompletedTask);

        var processor = new TickProcessorService(
            dedup.Object,
            repository.Object,
            Options.Create(new PipelineOptions { BatchSize = 10, FlushIntervalMs = 25 }),
            NullLogger<TickProcessorService>.Instance);
        var channel = Channel.CreateUnbounded<Tick>();
        var tick = new Tick("BTCUSDT", 10m, 1m, DateTimeOffset.UtcNow, "ExchangeA");

        await channel.Writer.WriteAsync(tick);
        await channel.Writer.WriteAsync(tick);
        channel.Writer.Complete();
        await processor.ProcessAsync(channel.Reader, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Single(persisted);
        Assert.Equal(1, processor.Snapshot().TotalDeduplicated);
    }
}
