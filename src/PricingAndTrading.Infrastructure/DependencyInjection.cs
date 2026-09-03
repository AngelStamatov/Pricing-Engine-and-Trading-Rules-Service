using Microsoft.Extensions.DependencyInjection;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Infrastructure.Pricing;
using PricingAndTrading.Infrastructure.RuntimeState;

namespace PricingAndTrading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<LatestPriceStore>();
        services.AddSingleton<ILatestPriceStore>(provider =>
            provider.GetRequiredService<LatestPriceStore>());
        services.AddSingleton<ILatestPriceProvider>(provider =>
            provider.GetRequiredService<LatestPriceStore>());

        services.AddSingleton<IPriceProcessor, PriceProcessor>();

        services.AddSingleton<ChannelPriceFeed>();
        services.AddSingleton<IPriceFeed>(provider =>
            provider.GetRequiredService<ChannelPriceFeed>());
        services.AddSingleton<IPriceTickPublisher>(provider =>
            provider.GetRequiredService<ChannelPriceFeed>());
        services.AddSingleton<SimulatedPriceGenerator>();

        services.AddHostedService<PriceProcessingBackgroundService>();
        services.AddHostedService<PriceGenerationBackgroundService>();

        return services;
    }
}
