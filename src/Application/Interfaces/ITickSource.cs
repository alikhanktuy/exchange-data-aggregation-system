using System.Threading.Channels;
using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Produces normalized market ticks from an external source.</summary>
public interface ITickSource
{
    /// <summary>Logical source name.</summary>
    string Name { get; }

    /// <summary>Starts receiving ticks and writing them to the supplied channel.</summary>
    Task StartAsync(ChannelWriter<Tick> writer, CancellationToken ct);
}
