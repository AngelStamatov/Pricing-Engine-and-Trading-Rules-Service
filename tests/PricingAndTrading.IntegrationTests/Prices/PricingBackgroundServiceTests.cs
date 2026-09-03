using System.Runtime.CompilerServices;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Pricing;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class PricingBackgroundServiceTests
{
    [Fact]
    public void PriceGenerationBackgroundService_ApplicationPublisher_CreatesService()
    {
        IPriceTickPublisher publisher = new StubPriceTickPublisher();

        var service = new PriceGenerationBackgroundService(
            publisher,
            new SimulatedPriceGenerator());

        Assert.NotNull(service);
    }

    [Fact]
    public void PriceGenerationBackgroundService_NullPublisher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PriceGenerationBackgroundService(
                null!,
                new SimulatedPriceGenerator()));
    }

    [Fact]
    public void PriceGenerationBackgroundService_NullGenerator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PriceGenerationBackgroundService(
                new StubPriceTickPublisher(),
                null!));
    }

    [Fact]
    public void PriceProcessingBackgroundService_NullFeed_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PriceProcessingBackgroundService(
                null!,
                new StubPriceProcessor()));
    }

    [Fact]
    public void PriceProcessingBackgroundService_NullProcessor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PriceProcessingBackgroundService(
                new StubPriceFeed(),
                null!));
    }

    private sealed class StubPriceTickPublisher : IPriceTickPublisher
    {
        public ValueTask PublishAsync(
            PriceTick priceTick,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private sealed class StubPriceFeed : IPriceFeed
    {
        public async IAsyncEnumerable<PriceTick> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class StubPriceProcessor : IPriceProcessor
    {
        public ValueTask ProcessAsync(
            PriceTick priceTick,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
