namespace Domain.Entities;

/// <summary>Immutable market tick received from an exchange.</summary>
public sealed record Tick(
    string Ticker,
    decimal Price,
    decimal Volume,
    DateTimeOffset Timestamp,
    string Source);
