
using Carter;
using CSharpFunctionalExtensions;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newsletter.Api.Database;
using Newsletter.Api.Extensions;
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
}, serviceName: "Newsletter-API");

// Dienstkonfiguration
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();


if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Apply migrations");
    app.ApplyMigrations((exception, retry) => logger.LogWarning("Migration failed {Retry} times. Problem {Problem}", retry, exception));

    logger.LogInformation("Use Swagger");
    app.UseSwagger();
    logger.LogInformation("Use Swagger UI");
    app.UseSwaggerUI();
}


// app.UseHttpsRedirection();
logger.LogInformation("Map Carter");
app.MapCarter();
logger.LogInformation("Map Default Endpoints");
app.MapDefaultEndpoints();

logger.LogInformation("Run");
app.Run();


