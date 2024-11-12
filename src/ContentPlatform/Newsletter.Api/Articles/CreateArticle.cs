using Carter;
using Contracts;
using FluentValidation;
using Mapster;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Newsletter.Api.Database;
using Newsletter.Api.Entities;
using Newsletter.Api.Shared;
using OpenTelemetry.Trace;
using Polly;
using System.Diagnostics;

namespace Newsletter.Api.Articles;

public static class CreateArticle
{
    public class Request
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
    }

    public class Command : IRequest<Result<Guid>>
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public Activity Activity { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Title).NotEmpty();
            RuleFor(c => c.Content).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<Guid>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<Handler> _logger;
        private readonly Instrumentation _diagnosticsConfig;

        public Handler(
            ApplicationDbContext dbContext,
            IValidator<Command> validator,
            IPublishEndpoint publishEndpoint,
            ILogger<Handler> logger,
            Instrumentation diagnosticsConfig)
        {
            _dbContext = dbContext;
            _validator = validator;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            _diagnosticsConfig = diagnosticsConfig;
        }

        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return Result.Failure<Guid>(new Error(
                    "CreateArticle.Validation",
                    validationResult.ToString()));
            }

            _logger.LogInformation("Validation is ok");

            using (var activity = _diagnosticsConfig.Source.StartActivity(
                "Create Article",
                kind: ActivityKind.Internal,
                request.Activity.Id,
                tags:
                [
                    new ("article.title", request.Title ),
                    new ("article.tags", request.Tags ),
                    new ("article.content", request.Content ),
                ]))
            {
                try
                {
                    if (request.Title == "boring article")
                    {
                        using var activityBoring = _diagnosticsConfig.Source.StartActivity(
                            "Handle boring article",
                            kind: ActivityKind.Internal,
                            Activity.Current?.Id);

                        await Task.Delay(1500);
                    }

                    if (request.Title == "faulty article")
                    {
                        throw new ArgumentException("Title not is invalid");
                    }

                    var article = new Article
                    {
                        Id = Guid.NewGuid(),
                        Title = request.Title,
                        Content = request.Content,
                        Tags = request.Tags,
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    activity?.SetTag("article.id", article.Id);
                    _dbContext.Add(article);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Article written to DB. {Article}", article);


                    await _publishEndpoint.Publish(
                        new ArticleCreatedEvent
                        {
                            Id = article.Id,
                            CreatedOnUtc = article.CreatedOnUtc
                        },
                        cancellationToken);
                    _logger.LogInformation("Article published to bus");


                    return article.Id;
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Something bad happened!");
                    activity?.RecordException(ex);
                    throw;
                }
            }
        }
    }
}

public class CreateArticleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/articles", async (HttpContext context, CreateArticle.Request request, ISender sender) =>
        {
            var command = request.Adapt<CreateArticle.Command>();
            command.Activity = context.Features.GetRequiredFeature<IHttpActivityFeature>()?.Activity;

            var result = await sender.Send(command);

            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }

            return Results.Ok(result.Value);
        });
    }
}
