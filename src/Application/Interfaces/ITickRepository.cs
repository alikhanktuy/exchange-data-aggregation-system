using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Persists market ticks.</summary>
public interface ITickRepository
{
    /// <summary>Inserts a batch of ticks.</summary>
    Task InsertBatchAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct = default);
}
