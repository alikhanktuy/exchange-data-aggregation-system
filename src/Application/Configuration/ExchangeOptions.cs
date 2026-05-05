namespace Application.Configuration;

/// <summary>Configuration for a single exchange WebSocket endpoint.</summary>
public sealed class ExchangeOptions
{
    /// <summary>Logical exchange name used as the tick source.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>WebSocket URL for the exchange stream.</summary>
    public string Url { get; init; } = string.Empty;
}
