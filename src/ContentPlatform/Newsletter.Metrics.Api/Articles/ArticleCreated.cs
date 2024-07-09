using Contracts;
using MassTransit;

namespace Newsletter.Metrics.Api.Articles;

public sealed class ArticleCreatedConsumer : IConsumer<ArticleCreatedEvent>
{
    public Task Consume(ConsumeContext<ArticleCreatedEvent> context)
    {
        DiagnosticsConfig.ArticlesCreatedCounter.Add(1);
        DiagnosticsConfig.Meter.CreateHistogram<int>("articles_created.histogram").Record(1);
        return Task.CompletedTask;
    }
}
