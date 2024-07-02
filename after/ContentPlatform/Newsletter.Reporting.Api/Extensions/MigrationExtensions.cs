using Microsoft.EntityFrameworkCore;
using Newsletter.Reporting.Api.Database;
using Polly;
using Polly.Retry;

namespace Newsletter.Reporting.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        RetryPolicy retryPolicy = Policy
           .Handle<Exception>()
           .WaitAndRetry(10, retryAttempt =>
               TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
               (exception, timeSpan, retryCount, context) =>
               {
               });

        retryPolicy.Execute(() => dbContext.Database.Migrate());
    }
}
