using Application.Configuration;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>Registers application-layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds application services and configuration options.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PipelineOptions>(configuration.GetSection("Pipeline"));
        services.AddSingleton<TickProcessorService>();
        services.AddSingleton<ITickProcessor>(sp => sp.GetRequiredService<TickProcessorService>());
        services.AddSingleton<ITickProcessorMetrics>(sp => sp.GetRequiredService<TickProcessorService>());
        services.AddSingleton<TickAggregatorService>();
        return services;
    }
}
