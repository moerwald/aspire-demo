using CSharpFunctionalExtensions;
using MassTransit;
using Newsletter.Metrics.Api;
using Newsletter.Metrics.Api.Articles;
using ServiceDefault;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults(
    traces => traces.AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName),
    meter => meter.AddMeter(DiagnosticsConfig.Meter.Name));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    busConfigurator.AddConsumer<ArticleCreatedConsumer>().Endpoint(e =>
    {
        e.InstanceId = "newsletter-metrics-api";
    });

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

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

