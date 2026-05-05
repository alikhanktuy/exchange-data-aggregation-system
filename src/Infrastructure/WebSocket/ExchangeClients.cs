using Application.Configuration;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.WebSocket;

/// <summary>ExchangeA WebSocket client.</summary>
public sealed class ExchangeAClient : WebSocketTickSource
{
    private readonly ITickNormalizer _normalizer;

    /// <summary>Creates an ExchangeA client.</summary>
    public ExchangeAClient(ExchangeOptions options, PipelineOptions pipelineOptions, ITickNormalizer normalizer, ILogger<ExchangeAClient> logger)
        : base(options, pipelineOptions, logger) => _normalizer = normalizer;

    /// <inheritdoc />
    protected override Tick? ParseMessage(string raw) => _normalizer.Normalize(raw, Name);
}

/// <summary>ExchangeB WebSocket client.</summary>
public sealed class ExchangeBClient : WebSocketTickSource
{
    private readonly ITickNormalizer _normalizer;

    /// <summary>Creates an ExchangeB client.</summary>
    public ExchangeBClient(ExchangeOptions options, PipelineOptions pipelineOptions, ITickNormalizer normalizer, ILogger<ExchangeBClient> logger)
        : base(options, pipelineOptions, logger) => _normalizer = normalizer;

    /// <inheritdoc />
    protected override Tick? ParseMessage(string raw) => _normalizer.Normalize(raw, Name);
}

/// <summary>ExchangeC WebSocket client.</summary>
public sealed class ExchangeCClient : WebSocketTickSource
{
    private readonly ITickNormalizer _normalizer;

    /// <summary>Creates an ExchangeC client.</summary>
    public ExchangeCClient(ExchangeOptions options, PipelineOptions pipelineOptions, ITickNormalizer normalizer, ILogger<ExchangeCClient> logger)
        : base(options, pipelineOptions, logger) => _normalizer = normalizer;

    /// <inheritdoc />
    protected override Tick? ParseMessage(string raw) => _normalizer.Normalize(raw, Name);
}
