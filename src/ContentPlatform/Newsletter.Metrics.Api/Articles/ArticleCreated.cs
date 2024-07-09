using Contracts;
using MassTransit;

namespace Newsletter.Metrics.Api.Articles;

public sealed class ArticleCreatedConsumer : IConsumer<ArticleCreatedEvent>
{
    private readonly ILogger<ArticleCreatedConsumer> _logger;

    public ArticleCreatedConsumer(ILogger<ArticleCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ArticleCreatedEvent> context)
    {
        _logger.LogInformation("Article created: {ArticleId}", context.Message.Id);
        DiagnosticsConfig.ArticlesCreatedCounter.Add(1);
        DiagnosticsConfig.Meter.CreateHistogram<int>("articles_created.histogram").Record(1);
        return Task.CompletedTask;
    }
}
