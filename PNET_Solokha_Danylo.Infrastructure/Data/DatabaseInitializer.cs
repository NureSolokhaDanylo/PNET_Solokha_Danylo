using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Infrastructure.Data;

public sealed class DatabaseInitializer(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Database initialization starting...");

        await WaitForSqlServerAsync(cancellationToken);

        logger.LogInformation("Applying migrations...");
        using var context = contextFactory.CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied successfully.");
    }

    private async Task WaitForSqlServerAsync(CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();
        var connectionString = context.Database.GetConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        var csb = new SqlConnectionStringBuilder(connectionString);
        var databaseName = csb.InitialCatalog;

        csb.InitialCatalog = "master";
        var masterConnectionString = csb.ConnectionString;
        var delay = TimeSpan.FromSeconds(5);
        var attempt = 1;

        logger.LogInformation("Waiting for SQL Server to be ready (Target: {DatabaseName})...", databaseName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync(cancellationToken);

                logger.LogInformation("SQL Server is responsive after {Attempt} attempts.", attempt);
                return;
            }
            catch (SqlException)
            {
                logger.LogWarning(
                    "SQL Server is not ready yet (Attempt {Attempt}). Retrying in {DelaySeconds}s...",
                    attempt,
                    delay.TotalSeconds);
            }

            attempt++;
            await Task.Delay(delay, cancellationToken);
        }
    }
}
