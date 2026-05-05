using System.Globalization;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Normalizers;

/// <summary>Parses ExchangeA flat JSON messages.</summary>
public sealed class ExchangeANormalizer : ITickNormalizer
{
    /// <inheritdoc />
    public Tick? Normalize(string raw, string source)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return new Tick(
                root.GetProperty("ticker").GetString() ?? string.Empty,
                root.GetProperty("price").GetDecimal(),
                root.GetProperty("volume").GetDecimal(),
                DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("ts").GetInt64()),
                source);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Parses ExchangeB nested JSON messages.</summary>
public sealed class ExchangeBNormalizer : ITickNormalizer
{
    /// <inheritdoc />
    public Tick? Normalize(string raw, string source)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var data = doc.RootElement.GetProperty("data");
            return new Tick(
                data.GetProperty("symbol").GetString() ?? string.Empty,
                decimal.Parse(data.GetProperty("p").GetString() ?? string.Empty, CultureInfo.InvariantCulture),
                decimal.Parse(data.GetProperty("v").GetString() ?? string.Empty, CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(data.GetProperty("time").GetString() ?? string.Empty, CultureInfo.InvariantCulture),
                source);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Parses ExchangeC CSV messages.</summary>
public sealed class ExchangeCNormalizer : ITickNormalizer
{
    /// <inheritdoc />
    public Tick? Normalize(string raw, string source)
    {
        try
        {
            var parts = raw.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4)
            {
                return null;
            }

            return new Tick(
                parts[0],
                decimal.Parse(parts[1], CultureInfo.InvariantCulture),
                decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[3], CultureInfo.InvariantCulture)),
                source);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Creates exchange-specific tick normalizers.</summary>
public sealed class TickNormalizerFactory
{
    /// <summary>Creates a normalizer for the configured exchange name.</summary>
    public ITickNormalizer Create(string exchangeName) => exchangeName switch
    {
        "ExchangeA" => new ExchangeANormalizer(),
        "ExchangeB" => new ExchangeBNormalizer(),
        "ExchangeC" => new ExchangeCNormalizer(),
        _ => throw new ArgumentOutOfRangeException(nameof(exchangeName), exchangeName, "Unsupported exchange.")
    };
}
