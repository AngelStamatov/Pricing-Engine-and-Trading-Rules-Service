namespace PricingAndTrading.Application.Orders.History;

public interface IOrderHistoryRepository
{
    Task<OrderHistoryPage> GetAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken = default);
}
