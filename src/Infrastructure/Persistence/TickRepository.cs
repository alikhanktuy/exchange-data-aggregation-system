using Application.Interfaces;
using Domain.Entities;
using NpgsqlTypes;

namespace Infrastructure.Persistence;

/// <summary>Persists ticks using PostgreSQL binary COPY.</summary>
public class TickRepository : ITickRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>Creates a tick repository.</summary>
    public TickRepository(DbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task InsertBatchAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct = default)
    {
        if (batch.Count == 0)
        {
            return;
        }

        await CopyAsync(batch, ct);
    }

    /// <summary>Performs the binary COPY operation. Overridable for focused unit tests.</summary>
    protected virtual async Task CopyAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.OpenConnectionAsync(ct);
        await using var writer = await conn.BeginBinaryImportAsync(
            "COPY ticks (ticker, price, volume, timestamp, source) FROM STDIN (FORMAT BINARY)",
            ct);

        foreach (var tick in batch)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(tick.Ticker, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(tick.Price, NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(tick.Volume, NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(tick.Timestamp, NpgsqlDbType.TimestampTz, ct);
            await writer.WriteAsync(tick.Source, NpgsqlDbType.Text, ct);
        }

        await writer.CompleteAsync(ct);
    }
}
