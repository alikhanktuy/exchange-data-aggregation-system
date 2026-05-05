using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SimulatedServer;

/// <summary>Hosts mock exchange WebSocket endpoints that emit randomized market ticks.</summary>
public static class MockExchangeServer
{
    private static readonly string[] Tickers = ["BTCUSDT", "ETHUSDT", "SOLUSDT", "ADAUSDT", "XRPUSDT"];
    private static readonly Dictionary<string, decimal> BasePrices = new()
    {
        ["BTCUSDT"] = 67_000m,
        ["ETHUSDT"] = 3_400m,
        ["SOLUSDT"] = 180m,
        ["ADAUSDT"] = 0.45m,
        ["XRPUSDT"] = 0.55m
    };

    /// <summary>Maps the mock exchange WebSocket endpoints.</summary>
    public static void MapEndpoints(WebApplication app)
    {
        app.Map("/ws/exchangeA", ctx => StreamAsync(ctx, "ExchangeA"));
        app.Map("/ws/exchangeB", ctx => StreamAsync(ctx, "ExchangeB"));
        app.Map("/ws/exchangeC", ctx => StreamAsync(ctx, "ExchangeC"));
    }

    private static async Task StreamAsync(HttpContext context, string exchange)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var random = Random.Shared;
        while (!context.RequestAborted.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var message = CreateMessage(exchange, random);
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, context.RequestAborted);
            await Task.Delay(random.Next(28, 51), context.RequestAborted);
        }
    }

    private static string CreateMessage(string exchange, Random random)
    {
        var ticker = Tickers[random.Next(Tickers.Length)];
        var price = Math.Round(BasePrices[ticker] * (1 + (decimal)((random.NextDouble() - 0.5) * 0.01)), 2);
        var volume = Math.Round((decimal)(random.NextDouble() * 10 + 0.01), 4);
        var timestamp = DateTimeOffset.UtcNow;

        return exchange switch
        {
            "ExchangeA" => JsonSerializer.Serialize(new
            {
                ticker,
                price,
                volume,
                ts = timestamp.ToUnixTimeMilliseconds()
            }),
            "ExchangeB" => JsonSerializer.Serialize(new
            {
                data = new
                {
                    symbol = ticker.Replace("USDT", "-USD", StringComparison.Ordinal),
                    p = price.ToString(CultureInfo.InvariantCulture),
                    v = volume.ToString(CultureInfo.InvariantCulture),
                    time = timestamp.ToString("O", CultureInfo.InvariantCulture)
                }
            }),
            "ExchangeC" => string.Join(',',
                ticker,
                price.ToString(CultureInfo.InvariantCulture),
                volume.ToString(CultureInfo.InvariantCulture),
                timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)),
            _ => throw new ArgumentOutOfRangeException(nameof(exchange), exchange, "Unsupported exchange.")
        };
    }
}
