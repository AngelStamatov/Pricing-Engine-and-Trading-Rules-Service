using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Api.Contracts.Requests;

public sealed class SubmitOrderRequest
{
    public required Guid OrderId { get; init; }

    public required string Symbol { get; init; }

    public required OrderSide Side { get; init; }

    public required OrderType Type { get; init; }

    public required decimal Price { get; init; }

    public required decimal Quantity { get; init; }
}
