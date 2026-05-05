namespace Application.Services;

/// <summary>Exposes tick processor counters for monitoring and tests.</summary>
public interface ITickProcessorMetrics
{
    /// <summary>Gets a consistent point-in-time metrics snapshot.</summary>
    TickProcessorMetrics Snapshot();
}
