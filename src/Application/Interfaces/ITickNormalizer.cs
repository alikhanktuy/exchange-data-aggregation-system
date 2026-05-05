using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Parses raw exchange payloads into normalized ticks.</summary>
public interface ITickNormalizer
{
    /// <summary>Attempts to parse a raw exchange message into a normalized tick.</summary>
    Tick? Normalize(string raw, string source);
}
