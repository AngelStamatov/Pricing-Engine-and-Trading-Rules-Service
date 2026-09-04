using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure.Persistence;

public static class PersistenceInitializer
{
    public static async Task InitializePersistenceAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        IDbContextFactory<TradingDbContext> dbContextFactory =
            services.GetRequiredService<IDbContextFactory<TradingDbContext>>();
        await using (TradingDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        ITradingRulesRepository tradingRulesRepository =
            services.GetRequiredService<ITradingRulesRepository>();
        ITradingRulesStore tradingRulesStore =
            services.GetRequiredService<ITradingRulesStore>();

        TradingRulesConfiguration? persistedTradingRules =
            await tradingRulesRepository.GetAsync(cancellationToken);

        if (persistedTradingRules is null)
        {
            await tradingRulesRepository.SaveAsync(
                tradingRulesStore.Current,
                cancellationToken);
        }
        else
        {
            tradingRulesStore.Update(persistedTradingRules);
        }

        IPriceStateRepository priceStateRepository =
            services.GetRequiredService<IPriceStateRepository>();
        ILatestPriceStore latestPriceStore =
            services.GetRequiredService<ILatestPriceStore>();
        IReadOnlyList<MarketPrice> persistedPrices =
            await priceStateRepository.GetAllAsync(cancellationToken);

        foreach (MarketPrice persistedPrice in persistedPrices)
        {
            latestPriceStore.Update(persistedPrice);
        }
    }
}
