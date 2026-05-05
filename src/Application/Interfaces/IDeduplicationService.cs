using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Provides short-window tick deduplication.</summary>
public interface IDeduplicationService
{
    /// <summary>Returns true if tick is a duplicate and should be dropped.</summary>
    Task<bool> IsDuplicateAsync(Tick tick, CancellationToken ct = default);
}
