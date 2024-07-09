IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);


var databaseNewsLetterApiDb =
    builder
    .AddPostgres("postgresDatabaseNewsLetterReportingApi")
    .WithPgAdmin()
    .AddDatabase("NewsletterDb");

var databaseNewsLetterReportingApiDb =
    builder
    .AddPostgres("postgresDatabaseNewsLetterApi")
    .WithPgAdmin()
    .AddDatabase("NewsletterReportingDb");

var rabbitmq =
    builder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

builder.AddProject<Projects.Newsletter_Api>("newsletterApi")
       .WithReference(databaseNewsLetterApiDb)
       .WithReference(rabbitmq);

builder.AddProject<Projects.Newsletter_Reporting_Api>("newsletterReportingApi")
    .WithReference(databaseNewsLetterReportingApiDb)
    .WithReference(rabbitmq);

builder.AddProject<Projects.Newsletter_Metrics_Api>("newsletterMetricsApi")
    .WithReference(rabbitmq);

builder.Build().Run();
