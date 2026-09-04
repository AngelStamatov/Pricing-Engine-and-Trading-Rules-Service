using Microsoft.EntityFrameworkCore;
using Npgsql;
using PricingAndTrading.Infrastructure.Persistence;

namespace PricingAndTrading.IntegrationTests.Persistence.PostgreSql;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable =
        "PRICING_TRADING_TEST_DB";
    public const string TestDatabaseName = "pricing_trading_tests";

    private string? _adminConnectionString;
    private string? _testConnectionString;

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable));

    public Task InitializeAsync()
    {
        if (!IsConfigured)
        {
            return Task.CompletedTask;
        }

        string sourceConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)!;

        var adminBuilder = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        _adminConnectionString = adminBuilder.ConnectionString;

        var testBuilder = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = TestDatabaseName
        };
        _testConnectionString = testBuilder.ConnectionString;

        return RecreateAndMigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_adminConnectionString is null)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await DropTestDatabaseAsync();
    }

    public IDbContextFactory<TradingDbContext> CreateDbContextFactory()
    {
        if (_testConnectionString is null)
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} is not configured.");
        }

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new TestDbContextFactory(options);
    }

    public async Task RecreateAndMigrateAsync()
    {
        EnsureConfigured();
        NpgsqlConnection.ClearAllPools();

        await DropTestDatabaseAsync();
        await using (var connection = new NpgsqlConnection(_adminConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "CREATE DATABASE \"pricing_trading_tests\";",
                connection);
            await command.ExecuteNonQueryAsync();
        }

        await using TradingDbContext dbContext =
            await CreateDbContextFactory().CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDataAsync()
    {
        EnsureConfigured();

        await using TradingDbContext dbContext =
            await CreateDbContextFactory().CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "OrderRejectionReasons",
                "Orders",
                "OrderIdRegistrations",
                "TradingRules",
                "LatestPrices"
            CASCADE;
            """);
    }

    private async Task DropTestDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = new NpgsqlCommand(
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @databaseName
              AND pid <> pg_backend_pid();
            """,
            connection))
        {
            terminateCommand.Parameters.AddWithValue(
                "databaseName",
                TestDatabaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = new NpgsqlCommand(
            "DROP DATABASE IF EXISTS \"pricing_trading_tests\";",
            connection);
        await dropCommand.ExecuteNonQueryAsync();
    }

    private void EnsureConfigured()
    {
        if (_adminConnectionString is null || _testConnectionString is null)
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} is not configured.");
        }
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<TradingDbContext> options) :
        IDbContextFactory<TradingDbContext>
    {
        public TradingDbContext CreateDbContext() => new(options);
    }
}
