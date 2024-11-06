using Microsoft.EntityFrameworkCore;
using Newsletter.Api.Database;
using Polly;
using Polly.Retry;

namespace Newsletter.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app, Action<Exception, int> logRetry)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        RetryPolicy retryPolicy = Policy
           .Handle<Exception>()
           .WaitAndRetry(3, retryAttempt =>
               TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
               (exception, timeSpan, retryCount, context) =>
               {
                   logRetry(exception, retryCount);
               });

        retryPolicy.Execute(() => dbContext.Database.Migrate());
    }
}
