using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Orders;

namespace PricingAndTrading.Infrastructure.Persistence.Repositories;

internal sealed class PostgresOrderIdRegistry : IOrderIdRegistry
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public PostgresOrderIdRegistry(
        IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    public async ValueTask<bool> TryRegisterAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID must not be empty.", nameof(orderId));
        }

        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // The unique key plus ON CONFLICT makes registration atomic across writers.
        int affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "OrderIdRegistrations" ("OrderId", "RegisteredAt")
            VALUES ({orderId}, {DateTimeOffset.UtcNow})
            ON CONFLICT ("OrderId") DO NOTHING
            """,
            cancellationToken);

        return affectedRows == 1;
    }
}
