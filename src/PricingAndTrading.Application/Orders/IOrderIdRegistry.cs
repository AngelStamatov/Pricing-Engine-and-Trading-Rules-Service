namespace PricingAndTrading.Application.Orders;

public interface IOrderIdRegistry
{
    /// <summary>
    /// Atomically registers an order ID if it is not already known.
    /// Implementations must make this operation concurrency-safe.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the ID was newly registered;
    /// otherwise, <see langword="false"/> when it was already known.
    /// </returns>
    ValueTask<bool> TryRegisterAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}
