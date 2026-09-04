using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.AutoTrading;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Infrastructure;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Pricing;
using PricingAndTrading.Infrastructure.RuntimeState;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class PricingDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_PriceFeedContracts_ResolveSameSingletonInstance()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();

        ChannelPriceFeed concreteFeed = provider.GetRequiredService<ChannelPriceFeed>();
        IPriceFeed consumer = provider.GetRequiredService<IPriceFeed>();
        IPriceTickPublisher publisher = provider.GetRequiredService<IPriceTickPublisher>();

        Assert.Same(concreteFeed, consumer);
        Assert.Same(concreteFeed, publisher);
    }

    [Fact]
    public void AddInfrastructure_AutoTradingEngine_ResolvesImplementation()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();

        IAutoTradingEngine engine = provider.GetRequiredService<IAutoTradingEngine>();

        Assert.IsType<AutoTradingEngine>(engine);
    }

    [Fact]
    public void AddInfrastructure_PersistenceAndRuntimeServices_FormsValidSingletonGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var latestPriceStore = provider.GetRequiredService<LatestPriceStore>();
        Assert.Same(
            latestPriceStore,
            provider.GetRequiredService<ILatestPriceSnapshotProvider>());

        var tradingRulesStore = provider.GetRequiredService<TradingRulesStore>();
        Assert.Same(
            tradingRulesStore,
            provider.GetRequiredService<ITradingRulesStore>());
        Assert.Same(
            tradingRulesStore,
            provider.GetRequiredService<ITradingRulesProvider>());

        Assert.NotNull(
            provider.GetRequiredService<IDbContextFactory<TradingDbContext>>());
        Assert.NotNull(provider.GetRequiredService<IOrderRepository>());
        Assert.NotNull(provider.GetRequiredService<IOrderHistoryRepository>());
        Assert.NotNull(provider.GetRequiredService<IOrderIdRegistry>());
        Assert.NotNull(provider.GetRequiredService<ITradingRulesRepository>());
        Assert.NotNull(provider.GetRequiredService<IPriceStateRepository>());
        Assert.NotNull(provider.GetRequiredService<IOrderProcessor>());
        Assert.NotNull(provider.GetRequiredService<IPriceProcessor>());
        Assert.NotNull(provider.GetRequiredService<ITradingRulesUpdater>());
    }

    [Fact]
    public void AddInfrastructure_MissingTradingRulesConfiguration_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TradingDatabase"] =
                    "Host=localhost;Database=pricing_and_trading;Username=pricing;Password=pricing"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(configuration));
    }

    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:TradingDatabase"] =
                "Host=localhost;Database=pricing_and_trading;Username=pricing;Password=pricing",
            ["TradingRules:MaximumNotionalAmount"] = "100000",
            ["TradingRules:MaximumQuantity"] = "10000",
            ["TradingRules:MaximumPriceDeviationPercent"] = "0.8",
            ["TradingRules:DuplicateOrderIdCheckEnabled"] = "true",
            ["TradingRules:SymbolWhitelistEnabled"] = "false",
            ["TradingRules:SymbolWhitelist:0"] = "EURUSD",
            ["TradingRules:AutoTradingSpreadThresholdPercent"] = "0.02"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
