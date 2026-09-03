using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Application.Abstractions;

public interface IOrderRepository
{
    Task SaveAsync(
        Order order,
        TradeDecision decision,
        CancellationToken cancellationToken);
}
