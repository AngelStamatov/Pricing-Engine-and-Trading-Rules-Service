using PricingAndTrading.Domain.Common;

namespace PricingAndTrading.Domain.Orders;

public sealed class Order
{
    public Order(
        Guid id,
        string symbol,
        OrderSide side,
        OrderType type,
        decimal price,
        decimal quantity,
        OrderSource source,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order ID must not be empty.", nameof(id));
        }

        Symbol = SymbolNormalizer.Normalize(symbol, nameof(symbol));

        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(
                nameof(side),
                side,
                "Order side must be a defined value.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Order type must be a defined value.");
        }

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Order source must be a defined value.");
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                price,
                "Price must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Quantity must be greater than zero.");
        }

        Id = id;
        Side = side;
        Type = type;
        Price = price;
        Quantity = quantity;
        Source = source;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Symbol { get; }

    public OrderSide Side { get; }

    public OrderType Type { get; }

    public decimal Price { get; }

    public decimal Quantity { get; }

    public OrderSource Source { get; }

    public DateTimeOffset CreatedAt { get; }
}
