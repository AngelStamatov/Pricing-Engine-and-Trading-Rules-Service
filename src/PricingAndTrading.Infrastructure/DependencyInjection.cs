using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.AutoTrading;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Repositories;
using PricingAndTrading.Infrastructure.Pricing;
using PricingAndTrading.Infrastructure.RuntimeState;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("TradingDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'TradingDatabase' is required.");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'TradingDatabase' must not be empty.");
        }

        TradingRulesConfiguration initialTradingRules =
            InitialTradingRulesOptions.CreateDomainConfiguration(configuration);

        services.AddPooledDbContextFactory<TradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<LatestPriceStore>();
        services.AddSingleton<ILatestPriceStore>(provider =>
            provider.GetRequiredService<LatestPriceStore>());
        services.AddSingleton<ILatestPriceProvider>(provider =>
            provider.GetRequiredService<LatestPriceStore>());
        services.AddSingleton<ILatestPriceSnapshotProvider>(provider =>
            provider.GetRequiredService<LatestPriceStore>());

        services.AddSingleton<TradingRulesStore>(_ =>
            new TradingRulesStore(initialTradingRules));
        services.AddSingleton<ITradingRulesStore>(provider =>
            provider.GetRequiredService<TradingRulesStore>());
        services.AddSingleton<ITradingRulesProvider>(provider =>
            provider.GetRequiredService<TradingRulesStore>());

        services.AddSingleton<IOrderRepository, PostgresOrderRepository>();
        services.AddSingleton<IOrderHistoryRepository, PostgresOrderHistoryRepository>();
        services.AddSingleton<IOrderIdRegistry, PostgresOrderIdRegistry>();
        services.AddSingleton<ITradingRulesRepository, PostgresTradingRulesRepository>();
        services.AddSingleton<IPriceStateRepository, PostgresPriceStateRepository>();

        services.AddSingleton<IPriceProcessor, PriceProcessor>();
        services.AddSingleton<IAutoTradingEngine, AutoTradingEngine>();
        services.AddSingleton<IOrderProcessor, OrderProcessor>();
        services.AddSingleton<ITradingRulesEngine, TradingRulesEngine>();
        services.AddSingleton<ITradingRulesUpdater, TradingRulesUpdater>();

        services.AddSingleton<ChannelPriceFeed>();
        services.AddSingleton<IPriceFeed>(provider =>
            provider.GetRequiredService<ChannelPriceFeed>());
        services.AddSingleton<IPriceTickPublisher>(provider =>
            provider.GetRequiredService<ChannelPriceFeed>());
        services.AddSingleton<SimulatedPriceGenerator>();

        services.AddHostedService<PriceProcessingBackgroundService>();
        services.AddHostedService<PriceGenerationBackgroundService>();
        services.AddHostedService<LatestPricePersistenceBackgroundService>();

        return services;
    }
}
