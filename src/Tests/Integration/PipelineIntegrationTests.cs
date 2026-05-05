using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Infrastructure.Normalizers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimulatedServer;

namespace Tests.Integration;

/// <summary>Integration-style tests for the processing pipeline.</summary>
public sealed class PipelineIntegrationTests
{
    /// <summary>Pipeline processes ticks from the in-process mock WebSocket server for five seconds.</summary>
    [Fact]
    public async Task PipelineProcessesTicksForFiveSeconds()
    {
        var repository = new CapturingRepository();
        var processor = new TickProcessorService(
            new MemoryDeduplicationService(),
            repository,
            Options.Create(new PipelineOptions { BatchSize = 50, FlushIntervalMs = 100 }),
            NullLogger<TickProcessorService>.Instance);
        var channel = Channel.CreateBounded<Tick>(1000);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var processing = processor.ProcessAsync(channel.Reader, cts.Token);

        await using var factory = new WebApplicationFactory<Program>();
        var server = factory.Server;
        var sourceTasks = new[]
        {
            ReadFromExchangeAsync(server, "/ws/exchangeA", "ExchangeA", new ExchangeANormalizer(), channel.Writer, cts.Token),
            ReadFromExchangeAsync(server, "/ws/exchangeB", "ExchangeB", new ExchangeBNormalizer(), channel.Writer, cts.Token),
            ReadFromExchangeAsync(server, "/ws/exchangeC", "ExchangeC", new ExchangeCNormalizer(), channel.Writer, cts.Token)
        };

        await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
        await Task.WhenAll(sourceTasks);
        channel.Writer.Complete();
        await processing;

        var snapshot = processor.Snapshot();
        Assert.True(snapshot.TotalInserted >= 200);
        Assert.Equal(0, snapshot.TotalErrors);
        Assert.Equal(repository.AllTicks.Count, repository.AllTicks.Distinct().Count());
    }

    private static async Task ReadFromExchangeAsync(
        TestServer server,
        string path,
        string source,
        ITickNormalizer normalizer,
        ChannelWriter<Tick> writer,
        CancellationToken ct)
    {
        var client = server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(new Uri($"ws://localhost{path}"), ct);
        var buffer = new byte[8192];

        while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var tick = normalizer.Normalize(raw, source);
                if (tick is not null)
                {
                    await writer.WriteAsync(tick, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private sealed class CapturingRepository : ITickRepository
    {
        public ConcurrentBag<Tick> AllTicks { get; } = [];

        public Task InsertBatchAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct = default)
        {
            foreach (var tick in batch)
            {
                AllTicks.Add(tick);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MemoryDeduplicationService : IDeduplicationService
    {
        private readonly ConcurrentDictionary<string, byte> _keys = new();

        public Task<bool> IsDuplicateAsync(Tick tick, CancellationToken ct = default)
        {
            var key = $"{tick.Source}:{tick.Ticker}:{tick.Timestamp.ToUnixTimeMilliseconds()}";
            return Task.FromResult(!_keys.TryAdd(key, 0));
        }
    }
}
