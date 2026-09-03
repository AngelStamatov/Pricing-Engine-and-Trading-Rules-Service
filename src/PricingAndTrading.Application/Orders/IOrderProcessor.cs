using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Application.Orders;

public interface IOrderProcessor
{
    Task<TradeDecision> ProcessAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
