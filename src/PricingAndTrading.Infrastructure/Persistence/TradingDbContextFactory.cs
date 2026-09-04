using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PricingAndTrading.Infrastructure.Persistence;

public sealed class TradingDbContextFactory :
    IDesignTimeDbContextFactory<TradingDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__TradingDatabase";

    public TradingDbContext CreateDbContext(string[] args)
    {
        string? connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringEnvironmentVariable}' is required for EF design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TradingDbContext(optionsBuilder.Options);
    }
}
