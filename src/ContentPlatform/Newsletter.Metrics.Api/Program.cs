using MassTransit;
using Newsletter.Metrics.Api;
using Newsletter.Metrics.Api.Articles;

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
        e.InstanceId = "article-created-newsletter-metrics-api";
    });

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

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

