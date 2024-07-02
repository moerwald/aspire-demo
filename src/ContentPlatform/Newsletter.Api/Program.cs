
using Carter;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newsletter.Api.Database;
using Newsletter.Api.Extensions;
using Npgsql;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(metrics =>
{
    metrics
    .AddSqlClientInstrumentation()
    .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
    .AddNpgsql();
});


// Dienstkonfiguration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(o =>
{
    var cs = builder.Configuration.GetConnectionString("NewsletterDb");
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
        var cfg = context.GetRequiredService<IConfiguration>();
        var uri = cfg.GetConnectionString("rabbitmq");

        var uriObj = new Uri(uri);
        string scheme = uriObj.Scheme; // "amqp"
        string userInfo = uriObj.UserInfo; // "guest:UvEruDWYZSeC2skbsZTdWp"
        string host = uriObj.Host; // "localhost"
        int port = uriObj.Port; // 41695

        // Split userInfo to get username and password
        var userInfoParts = userInfo.Split(':');
        string username = userInfoParts[0]; // "guest"
        string password = userInfoParts.Length > 1 ? userInfoParts[1] : string.Empty; // "UvEruDWYZSeC2skbsZTdWp"

        configurator.Host(uriObj, h =>
        {
            h.Username(username);
            h.Password(password);
        });

        configurator.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrations();
}

app.UseHttpsRedirection();
app.MapCarter();

app.Run();


