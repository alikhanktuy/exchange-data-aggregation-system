using Infrastructure.Normalizers;

namespace Tests.Unit;

/// <summary>Unit tests for exchange message normalizers.</summary>
public sealed class TickNormalizerTests
{
    /// <summary>ExchangeA flat JSON is parsed into a tick.</summary>
    [Fact]
    public void ExchangeAValidInputReturnsTick()
    {
        var tick = new ExchangeANormalizer().Normalize(
            """{"ticker":"BTCUSDT","price":67123.45,"volume":0.5,"ts":1713000000000}""",
            "ExchangeA");

        Assert.NotNull(tick);
        Assert.Equal("BTCUSDT", tick.Ticker);
        Assert.Equal(67123.45m, tick.Price);
        Assert.Equal(0.5m, tick.Volume);
        Assert.Equal("ExchangeA", tick.Source);
    }

    /// <summary>ExchangeB nested JSON is parsed into a tick.</summary>
    [Fact]
    public void ExchangeBValidInputReturnsTick()
    {
        var tick = new ExchangeBNormalizer().Normalize(
            """{"data":{"symbol":"ETH-USD","p":"3456.78","v":"1.2","time":"2024-04-13T12:00:00Z"}}""",
            "ExchangeB");

        Assert.NotNull(tick);
        Assert.Equal("ETH-USD", tick.Ticker);
        Assert.Equal(3456.78m, tick.Price);
        Assert.Equal(1.2m, tick.Volume);
    }

    /// <summary>ExchangeC CSV is parsed into a tick.</summary>
    [Fact]
    public void ExchangeCValidInputReturnsTick()
    {
        var tick = new ExchangeCNormalizer().Normalize("SOLUSDT,180.55,10.0,1713000001000", "ExchangeC");

        Assert.NotNull(tick);
        Assert.Equal("SOLUSDT", tick.Ticker);
        Assert.Equal(180.55m, tick.Price);
        Assert.Equal(10.0m, tick.Volume);
    }

    /// <summary>Malformed payloads return null and do not throw.</summary>
    [Theory]
    [InlineData("ExchangeA")]
    [InlineData("ExchangeB")]
    [InlineData("ExchangeC")]
    public void MalformedInputReturnsNull(string exchange)
    {
        var normalizer = new TickNormalizerFactory().Create(exchange);
        Assert.Null(normalizer.Normalize("bad payload", exchange));
    }
}
