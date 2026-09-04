namespace PricingAndTrading.Infrastructure.Persistence.Entities;

internal sealed class OrderIdRegistrationEntity
{
    public Guid OrderId { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }
}
