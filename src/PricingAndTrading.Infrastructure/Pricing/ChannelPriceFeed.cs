using System.Threading.Channels;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Infrastructure.Pricing;

public sealed class ChannelPriceFeed : IPriceFeed, IPriceTickPublisher
{
    public const int DefaultCapacity = 1_024;

    private readonly Channel<PriceTick> _channel;

    public ChannelPriceFeed(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _channel = Channel.CreateBounded<PriceTick>(new BoundedChannelOptions(capacity)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask PublishAsync(
        PriceTick priceTick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priceTick);
        return _channel.Writer.WriteAsync(priceTick, cancellationToken);
    }

    public IAsyncEnumerable<PriceTick> ReadAllAsync(
        CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
