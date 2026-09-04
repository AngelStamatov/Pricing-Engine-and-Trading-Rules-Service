using System.Collections.Concurrent;
using PricingAndTrading.Infrastructure.RuntimeState;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.RuntimeState;

public sealed class TradingRulesStoreTests
{
    [Fact]
    public void Current_InitialSnapshot_ReturnsConfiguredSnapshot()
    {
        TradingRulesConfiguration initial = CreateTradingRules(100_000m);
        var store = new TradingRulesStore(initial);

        TradingRulesConfiguration current = store.Current;

        Assert.Same(initial, current);
    }

    [Fact]
    public void Update_NewSnapshot_AtomicallyReplacesActiveReference()
    {
        TradingRulesConfiguration initial = CreateTradingRules(100_000m);
        TradingRulesConfiguration replacement = CreateTradingRules(200_000m);
        var store = new TradingRulesStore(initial);

        store.Update(replacement);

        Assert.Same(replacement, store.Current);
    }

    [Fact]
    public void Update_ConcurrentReaders_ObserveOnlyCompleteSnapshots()
    {
        TradingRulesConfiguration initial = CreateTradingRules(100_000m);
        TradingRulesConfiguration replacement = CreateTradingRules(200_000m);
        var store = new TradingRulesStore(initial);
        var invalidSnapshots = new ConcurrentQueue<TradingRulesConfiguration>();

        Parallel.For(0, 10_000, index =>
        {
            if (index % 3 == 0)
            {
                store.Update(index % 2 == 0 ? initial : replacement);
                return;
            }

            TradingRulesConfiguration observed = store.Current;
            if (!ReferenceEquals(observed, initial)
                && !ReferenceEquals(observed, replacement))
            {
                invalidSnapshots.Enqueue(observed);
            }
        });

        Assert.Empty(invalidSnapshots);
    }

    private static TradingRulesConfiguration CreateTradingRules(
        decimal maximumNotionalAmount) =>
        new(
            maximumNotionalAmount,
            maximumQuantity: 10_000m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: false,
            symbolWhitelist: ["EURUSD"],
            autoTradingSpreadThresholdPercent: 0.02m,
            maximumPriceDeviationPercent: 0.8m);
}
