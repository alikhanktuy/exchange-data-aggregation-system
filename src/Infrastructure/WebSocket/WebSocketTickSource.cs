using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Application.Configuration;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.WebSocket;

/// <summary>Base WebSocket tick source with reconnect loop and bounded-channel publishing.</summary>
public abstract class WebSocketTickSource : ITickSource
{
    private readonly Uri _uri;
    private readonly ILogger _logger;
    private readonly int _maxRetries;

    /// <summary>Initializes a WebSocket tick source.</summary>
    protected WebSocketTickSource(ExchangeOptions options, PipelineOptions pipelineOptions, ILogger logger)
    {
        Name = options.Name;
        _uri = new Uri(options.Url);
        _logger = logger;
        _maxRetries = pipelineOptions.ReconnectMaxRetries;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task StartAsync(ChannelWriter<Tick> writer, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                await socket.ConnectAsync(_uri, ct);
                _logger.LogInformation("Connected | Source: {Source} | Url: {Url}", Name, _uri);
                attempt = 0;
                await ReceiveLoopAsync(socket, writer, ct);
                _logger.LogWarning("Disconnected | Source: {Source}", Name);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Disconnected | Source: {Source}", Name);
            }

            attempt++;
            if (attempt > _maxRetries)
            {
                _logger.LogCritical("Reconnect attempts exhausted | Source: {Source}", Name);
                return;
            }

            var delay = TimeSpan.FromSeconds(Math.Min(16, Math.Pow(2, attempt - 1)));
            _logger.LogWarning("Reconnect attempt {Attempt} | Source: {Source} | Delay: {Delay}", attempt, Name, delay);
            await Task.Delay(delay, ct);
        }
    }

    /// <summary>Parses a raw message into a tick, or returns null for malformed data.</summary>
    protected abstract Tick? ParseMessage(string raw);

    private async Task ReceiveLoopAsync(ClientWebSocket socket, ChannelWriter<Tick> writer, CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var raw = Encoding.UTF8.GetString(ms.ToArray());
            var tick = ParseMessage(raw);
            if (tick is null)
            {
                _logger.LogWarning("ParseError | Source: {Source} | Payload: {Payload}", Name, raw);
                continue;
            }

            if (!writer.TryWrite(tick))
            {
                _logger.LogWarning("Channel full; tick dropped by writer | Source: {Source}", Name);
            }
        }
    }
}
