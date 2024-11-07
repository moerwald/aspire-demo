using Carter;
using CSharpFunctionalExtensions;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newsletter.Reporting.Api.Articles;
using Newsletter.Reporting.Api.Database;
using Newsletter.Reporting.Api.Extensions;
using Npgsql;
using OpenTelemetry.Trace;
using ServiceDefault;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(metrics =>
{
    metrics
    .AddSqlClientInstrumentation()
    .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
    .AddNpgsql();
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(o =>
{
    var cs = builder.Configuration.GetConnectionString("Database");
    o.UseNpgsql(cs);
});

var assembly = typeof(Program).Assembly;

builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));

builder.Services.AddCarter();

builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    busConfigurator.AddConsumer<ArticleCreatedConsumer>().Endpoint(e =>
    {
        e.InstanceId = "-newsletter-reporting-api";
    });

    busConfigurator.AddConsumer<ArticleViewedConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        context
        .GetRequiredService<IConfiguration>()
        .ToResult("No config service registered")
        .Map(cfg =>
        {
            RabbitMqUri
            .From(cfg.GetConnectionString("rabbitmq"))
            .Map((RabbitMqUri rabbitMqUri) =>
            {
                configurator.Host(
                    rabbitMqUri.Uri, h =>
                {
                    h.Username(rabbitMqUri.Username);
                    h.Password(rabbitMqUri.Password);
                });
                configurator.ConfigureEndpoints(context);
                return UnitResult.Success<string>();
            });
            return UnitResult.Success<string>();
        })
        .Match(_ => { }, error => Console.WriteLine(error));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}

app.MapCarter();

app.UseHttpsRedirection();

app.Run();