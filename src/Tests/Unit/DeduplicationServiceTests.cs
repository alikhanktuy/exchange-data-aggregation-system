using Application.Configuration;
using Domain.Entities;
using Infrastructure.Deduplication;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Tests.Unit;

/// <summary>Unit tests for Redis deduplication behavior.</summary>
public sealed class DeduplicationServiceTests
{
    /// <summary>First SET NX success is not duplicate; second failure is duplicate.</summary>
    [Fact]
    public async Task StringSetNxResultMapsToDuplicateFlag()
    {
        var database = new Mock<IDatabase>();
        database
            .SetupSequence(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        var service = new RedisDeduplicationService(
            database.Object,
            Options.Create(new PipelineOptions { DeduplicationWindowSec = 5 }));
        var tick = new Tick("BTCUSDT", 10m, 1m, DateTimeOffset.UtcNow, "ExchangeA");

        Assert.False(await service.IsDuplicateAsync(tick));
        Assert.True(await service.IsDuplicateAsync(tick));
    }
}
