using System.Threading.Channels;
using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Consumes ticks, deduplicates them, and persists them in batches.</summary>
public interface ITickProcessor
{
    /// <summary>Processes ticks until cancellation or channel completion.</summary>
    Task ProcessAsync(ChannelReader<Tick> reader, CancellationToken ct);
}
