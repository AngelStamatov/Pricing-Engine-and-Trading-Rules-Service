using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Repositories;

internal sealed class PostgresOrderRepository : IOrderRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public PostgresOrderRepository(
        IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    public async Task SaveAsync(
        Order order,
        TradeDecision decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(decision);

        if (decision.OrderId != order.Id)
        {
            throw new ArgumentException(
                "The trade decision must refer to the supplied order.",
                nameof(decision));
        }

        OrderEntity entity = PersistenceMapper.ToEntity(
            order,
            decision,
            Guid.NewGuid());

        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Orders.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
