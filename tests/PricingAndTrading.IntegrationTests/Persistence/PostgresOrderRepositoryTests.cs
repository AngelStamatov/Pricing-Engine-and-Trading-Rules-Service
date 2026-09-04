using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Repositories;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class PostgresOrderRepositoryTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAsync_NullOrder_ThrowsArgumentNullExceptionBeforeDatabaseAccess()
    {
        var repository = new PostgresOrderRepository(new UnusedDbContextFactory());
        TradeDecision decision = TradeDecision.Accepted(Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!, decision, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_NullDecision_ThrowsArgumentNullExceptionBeforeDatabaseAccess()
    {
        var repository = new PostgresOrderRepository(new UnusedDbContextFactory());
        Order order = CreateOrder(Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(order, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_MismatchedDecisionOrderId_ThrowsArgumentExceptionBeforeDatabaseAccess()
    {
        var repository = new PostgresOrderRepository(new UnusedDbContextFactory());
        Order order = CreateOrder(Guid.NewGuid());
        TradeDecision decision = TradeDecision.Accepted(Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveAsync(order, decision, CancellationToken.None));
    }

    private static Order CreateOrder(Guid orderId) =>
        new(
            orderId,
            "EURUSD",
            OrderSide.Buy,
            OrderType.Limit,
            100m,
            5m,
            OrderSource.Api,
            Timestamp);

    private sealed class UnusedDbContextFactory : IDbContextFactory<TradingDbContext>
    {
        public TradingDbContext CreateDbContext() =>
            throw new InvalidOperationException("Database access was not expected.");
    }
}
