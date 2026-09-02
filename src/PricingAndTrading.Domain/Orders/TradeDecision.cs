namespace PricingAndTrading.Domain.Orders;

public sealed class TradeDecision
{
    private TradeDecision(
        Guid orderId,
        OrderStatus status,
        IReadOnlyList<RejectionReason> rejectionReasons)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID must not be empty.", nameof(orderId));
        }

        OrderId = orderId;
        Status = status;
        RejectionReasons = rejectionReasons;
    }

    public Guid OrderId { get; }

    public OrderStatus Status { get; }

    public IReadOnlyList<RejectionReason> RejectionReasons { get; }

    public static TradeDecision Accepted(Guid orderId)
    {
        return new TradeDecision(
            orderId,
            OrderStatus.Accepted,
            Array.Empty<RejectionReason>());
    }

    public static TradeDecision Rejected(
        Guid orderId,
        params RejectionReason[] rejectionReasons)
    {
        ArgumentNullException.ThrowIfNull(rejectionReasons);

        if (rejectionReasons.Length == 0)
        {
            throw new ArgumentException(
                "A rejected decision must contain at least one rejection reason.",
                nameof(rejectionReasons));
        }

        if (rejectionReasons.Any(static reason => reason is null))
        {
            throw new ArgumentException(
                "Rejection reasons must not contain null values.",
                nameof(rejectionReasons));
        }

        return new TradeDecision(
            orderId,
            OrderStatus.Rejected,
            Array.AsReadOnly(rejectionReasons.ToArray()));
    }
}
