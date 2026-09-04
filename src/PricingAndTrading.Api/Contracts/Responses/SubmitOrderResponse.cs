using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Api.Contracts.Responses;

public sealed record SubmitOrderResponse(
    Guid OrderId,
    OrderStatus Status,
    IReadOnlyList<RejectionReasonResponse> RejectionReasons);
