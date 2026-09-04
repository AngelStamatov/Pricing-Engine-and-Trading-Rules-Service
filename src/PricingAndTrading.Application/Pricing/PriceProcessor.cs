using PricingAndTrading.Application.AutoTrading;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.Pricing;

public sealed class PriceProcessor : IPriceProcessor
{
    private readonly ILatestPriceStore _latestPriceStore;
    private readonly ITradingRulesProvider _tradingRulesProvider;
    private readonly IAutoTradingEngine _autoTradingEngine;
    private readonly IOrderProcessor _orderProcessor;

    public PriceProcessor(
        ILatestPriceStore latestPriceStore,
        ITradingRulesProvider tradingRulesProvider,
        IAutoTradingEngine autoTradingEngine,
        IOrderProcessor orderProcessor)
    {
        ArgumentNullException.ThrowIfNull(latestPriceStore);
        ArgumentNullException.ThrowIfNull(tradingRulesProvider);
        ArgumentNullException.ThrowIfNull(autoTradingEngine);
        ArgumentNullException.ThrowIfNull(orderProcessor);

        _latestPriceStore = latestPriceStore;
        _tradingRulesProvider = tradingRulesProvider;
        _autoTradingEngine = autoTradingEngine;
        _orderProcessor = orderProcessor;
    }

    public async ValueTask ProcessAsync(
        PriceTick priceTick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priceTick);
        cancellationToken.ThrowIfCancellationRequested();

        MarketPrice current = MarketPrice.From(priceTick);
        MarketPrice? previous = _latestPriceStore.Update(current);
        TradingRulesConfiguration tradingRules = _tradingRulesProvider.Current;
        Order? autoOrder = _autoTradingEngine.Evaluate(
            previous,
            current,
            tradingRules);

        if (autoOrder is not null)
        {
            await _orderProcessor.ProcessAsync(autoOrder, cancellationToken);
        }
    }
}
