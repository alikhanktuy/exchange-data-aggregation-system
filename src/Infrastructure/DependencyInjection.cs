using Application.Configuration;
using Application.Interfaces;
using Infrastructure.Deduplication;
using Infrastructure.Normalizers;
using Infrastructure.Persistence;
using Infrastructure.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure;

/// <summary>Registers infrastructure services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds infrastructure services including WebSocket clients, Redis, and PostgreSQL persistence.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<ITickRepository, TickRepository>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.")));
        services.AddSingleton<IDeduplicationService, RedisDeduplicationService>();
        services.AddSingleton<TickNormalizerFactory>();

        var exchanges = configuration.GetSection("Exchanges").Get<ExchangeOptions[]>() ?? [];
        foreach (var exchange in exchanges)
        {
            services.AddSingleton<ITickSource>(sp => CreateSource(sp, exchange));
        }

        return services;
    }

    private static ITickSource CreateSource(IServiceProvider sp, ExchangeOptions exchange)
    {
        var pipeline = sp.GetRequiredService<IOptions<PipelineOptions>>().Value;
        var normalizer = sp.GetRequiredService<TickNormalizerFactory>().Create(exchange.Name);
        return exchange.Name switch
        {
            "ExchangeA" => new ExchangeAClient(exchange, pipeline, normalizer, sp.GetRequiredService<ILogger<ExchangeAClient>>()),
            "ExchangeB" => new ExchangeBClient(exchange, pipeline, normalizer, sp.GetRequiredService<ILogger<ExchangeBClient>>()),
            "ExchangeC" => new ExchangeCClient(exchange, pipeline, normalizer, sp.GetRequiredService<ILogger<ExchangeCClient>>()),
            _ => throw new ArgumentOutOfRangeException(nameof(exchange), exchange.Name, "Unsupported exchange.")
        };
    }
}
