using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Repositories;

internal sealed class PostgresOrderHistoryRepository : IOrderHistoryRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public PostgresOrderHistoryRepository(
        IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrderHistoryPage> GetAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<OrderEntity> filteredOrders = dbContext.Orders.AsNoTracking();

        if (query.Symbol is not null)
        {
            string symbol = query.Symbol;
            filteredOrders = filteredOrders.Where(
                order => order.Symbol == symbol);
        }

        if (query.Status is not null)
        {
            var status = query.Status.Value;
            filteredOrders = filteredOrders.Where(
                order => order.Status == status);
        }

        if (query.Source is not null)
        {
            var source = query.Source.Value;
            filteredOrders = filteredOrders.Where(
                order => order.Source == source);
        }

        if (query.From is not null)
        {
            DateTimeOffset from = query.From.Value;
            filteredOrders = filteredOrders.Where(
                order => order.CreatedAt >= from);
        }

        if (query.To is not null)
        {
            DateTimeOffset to = query.To.Value;
            filteredOrders = filteredOrders.Where(
                order => order.CreatedAt <= to);
        }

        int totalCount = await filteredOrders.CountAsync(cancellationToken);
        int skip = (int)Math.Min(
            (long)(query.Page - 1) * query.PageSize,
            int.MaxValue);

        List<OrderEntity> entities = await filteredOrders
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.PersistenceId)
            .Skip(skip)
            .Take(query.PageSize)
            .Include(order => order.RejectionReasons)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        OrderHistoryItem[] items = entities
            .Select(ToHistoryItem)
            .ToArray();

        return new OrderHistoryPage(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }

    private static OrderHistoryItem ToHistoryItem(OrderEntity entity) =>
        new(
            entity.OrderId,
            entity.Symbol,
            entity.Side,
            entity.Type,
            entity.Price,
            entity.Quantity,
            entity.Source,
            entity.CreatedAt,
            entity.Status,
            entity.RejectionReasons
                .OrderBy(reason => reason.Sequence)
                .Select(reason => new OrderHistoryRejectionReason(
                    reason.Code,
                    reason.Message)));
}
