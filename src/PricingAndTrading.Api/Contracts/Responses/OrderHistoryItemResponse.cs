using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Api.Contracts.Responses;

public sealed record OrderHistoryItemResponse(
    Guid OrderId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Price,
    decimal Quantity,
    OrderSource Source,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    IReadOnlyList<RejectionReasonResponse> RejectionReasons);
