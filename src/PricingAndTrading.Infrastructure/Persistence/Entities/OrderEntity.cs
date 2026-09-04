using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Infrastructure.Persistence.Entities;

internal sealed class OrderEntity
{
    public Guid PersistenceId { get; set; }

    public Guid OrderId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public OrderSide Side { get; set; }

    public OrderType Type { get; set; }

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public OrderSource Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public ICollection<OrderRejectionReasonEntity> RejectionReasons { get; } =
        new List<OrderRejectionReasonEntity>();
}
