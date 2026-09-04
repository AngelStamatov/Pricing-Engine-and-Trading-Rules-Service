namespace PricingAndTrading.IntegrationTests.Persistence.PostgreSql;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!PostgreSqlFixture.IsConfigured)
        {
            Skip = $"Set {PostgreSqlFixture.ConnectionStringEnvironmentVariable} to run real PostgreSQL tests.";
        }
    }
}
