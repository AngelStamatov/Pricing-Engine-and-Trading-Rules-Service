namespace PricingAndTrading.Infrastructure.Persistence.Entities;

internal sealed class OrderRejectionReasonEntity
{
    public Guid OrderPersistenceId { get; set; }

    public int Sequence { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public OrderEntity Order { get; set; } = null!;
}
