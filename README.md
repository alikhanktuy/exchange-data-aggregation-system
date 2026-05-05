# Exchange Data Aggregation System

Production-oriented .NET 8 market tick ingestion sample using Clean Architecture, WebSocket sources, Redis deduplication, and TimescaleDB binary COPY inserts.

## Quick Start

Prerequisites:

- Docker and Docker Compose
- .NET 8 SDK for local build/test workflows

Run everything:

```bash
docker compose up --build
```

The aggregator connects to three mock WebSocket exchanges and logs monitor counters every 10 seconds.

Check inserted rows:

```bash
docker exec -it exchangedataaggregationsystem-timescaledb-1 psql -U postgres -d ticks_db -c "select count(*) from ticks;"
```

## Architecture

```text
   ExchangeA JSON     ExchangeB JSON     ExchangeC CSV
        |                  |                 |
        v                  v                 v
  WebSocketTickSource reconnecting clients
        |                  |                 |
        +-------- bounded Channel<Tick> -----+
                         |
                         v
              TickProcessorService
                         |
        Redis SET NX TTL deduplication
                         |
                         v
        Batch 200 ticks OR 500 ms elapsed
                         |
                         v
          TickRepository BINARY COPY
                         |
                         v
              TimescaleDB hypertable
```

## Key Decisions

BINARY COPY is used because high-frequency tick ingestion is write-heavy and row-by-row inserts waste network round trips and server parse overhead. `Npgsql.BeginBinaryImport` keeps the database path compact and predictable.

Redis stores the deduplication window because duplicate detection is a short-lived, high-cardinality presence check. `SET key value NX EX` gives atomic first-seen behavior and automatic cleanup without database reads.

`Channel<Tick>` provides in-process backpressure between WebSocket clients and persistence. It is bounded and configured with `DropOldest`, which protects process memory under bursts while keeping recent market data flowing. A warning log documents this policy at startup.

## Adding An Exchange

Add a new `ITickNormalizer` implementation for the payload format, add a small `WebSocketTickSource` subclass that calls that normalizer, register it in `TickNormalizerFactory` and `Infrastructure.DependencyInjection`, then add `{ "Name": "...", "Url": "ws://..." }` to the `Exchanges` configuration.

## Requirement Coverage

- 2-3 simultaneous WebSocket clients: `ExchangeAClient`, `ExchangeBClient`, `ExchangeCClient`.
- Different source formats: flat JSON, nested JSON, CSV.
- Normalization: exchange-specific `ITickNormalizer` implementations.
- Duplicate removal: Redis `SET NX` with TTL in `RedisDeduplicationService`.
- Raw tick storage: `ticks` TimescaleDB hypertable.
- Efficient DB writes: batched Npgsql binary COPY.
- Reconnect handling: `WebSocketTickSource` exponential backoff.
- Monitoring: Serilog connection/error logs plus 10-second counter log.
- Tests: unit tests for parsers, deduplication, processor, repository; integration test with in-process WebSocket server.

## Benchmarks

Run the processor benchmark:

```bash
dotnet run -c Release --project src/Benchmarks/Benchmarks.csproj
```

The benchmark runs the real `TickProcessorService` with in-memory repository and deduplication to measure pipeline overhead for 100, 1,000, and 10,000 tick workloads. Docker/PostgreSQL/Redis are intentionally excluded from this benchmark so it measures application pipeline throughput without infrastructure noise.
