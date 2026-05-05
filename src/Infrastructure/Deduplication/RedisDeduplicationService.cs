using Application.Configuration;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Deduplication;

/// <summary>Redis SET NX deduplication service with a short TTL window.</summary>
public sealed class RedisDeduplicationService : IDeduplicationService
{
    private readonly IDatabase _database;
    private readonly TimeSpan _expiry;

    /// <summary>Creates a Redis deduplication service.</summary>
    public RedisDeduplicationService(IConnectionMultiplexer redis, IOptions<PipelineOptions> options)
        : this(redis.GetDatabase(), options)
    {
    }

    /// <summary>Creates a Redis deduplication service with an explicit database, primarily for tests.</summary>
    public RedisDeduplicationService(IDatabase database, IOptions<PipelineOptions> options)
    {
        _database = database;
        _expiry = TimeSpan.FromSeconds(options.Value.DeduplicationWindowSec);
    }

    /// <inheritdoc />
    public async Task<bool> IsDuplicateAsync(Tick tick, CancellationToken ct = default)
    {
        var key = $"dedup:{tick.Source}:{tick.Ticker}:{tick.Timestamp.ToUnixTimeMilliseconds()}";
        var wasSet = await _database.StringSetAsync(key, "1", _expiry, false, When.NotExists);
        return !wasSet;
    }
}
