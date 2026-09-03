using Microsoft.Extensions.DependencyInjection;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Infrastructure;
using PricingAndTrading.Infrastructure.Pricing;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class PricingDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_PriceFeedContracts_ResolveSameSingletonInstance()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        using ServiceProvider provider = services.BuildServiceProvider();

        ChannelPriceFeed concreteFeed = provider.GetRequiredService<ChannelPriceFeed>();
        IPriceFeed consumer = provider.GetRequiredService<IPriceFeed>();
        IPriceTickPublisher publisher = provider.GetRequiredService<IPriceTickPublisher>();

        Assert.Same(concreteFeed, consumer);
        Assert.Same(concreteFeed, publisher);
    }
}
