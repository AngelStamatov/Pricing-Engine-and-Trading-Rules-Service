using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Application.Orders.History;

public sealed class OrderHistoryItem
{
    public OrderHistoryItem(
        Guid orderId,
        string symbol,
        OrderSide side,
        OrderType type,
        decimal price,
        decimal quantity,
        OrderSource source,
        DateTimeOffset createdAt,
        OrderStatus status,
        IEnumerable<OrderHistoryRejectionReason> rejectionReasons)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(rejectionReasons);

        OrderId = orderId;
        Symbol = symbol;
        Side = side;
        Type = type;
        Price = price;
        Quantity = quantity;
        Source = source;
        CreatedAt = createdAt;
        Status = status;
        RejectionReasons = Array.AsReadOnly(rejectionReasons.ToArray());
    }

    public Guid OrderId { get; }

    public string Symbol { get; }

    public OrderSide Side { get; }

    public OrderType Type { get; }

    public decimal Price { get; }

    public decimal Quantity { get; }

    public OrderSource Source { get; }

    public DateTimeOffset CreatedAt { get; }

    public OrderStatus Status { get; }

    public IReadOnlyList<OrderHistoryRejectionReason> RejectionReasons { get; }
}
