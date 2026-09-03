using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.Orders;

public sealed class OrderProcessor : IOrderProcessor
{
    private readonly ITradingRulesProvider _tradingRulesProvider;
    private readonly ILatestPriceProvider _latestPriceProvider;
    private readonly IOrderIdRegistry _orderIdRegistry;
    private readonly ITradingRulesEngine _tradingRulesEngine;
    private readonly IOrderRepository _orderRepository;

    public OrderProcessor(
        ITradingRulesProvider tradingRulesProvider,
        ILatestPriceProvider latestPriceProvider,
        IOrderIdRegistry orderIdRegistry,
        ITradingRulesEngine tradingRulesEngine,
        IOrderRepository orderRepository)
    {
        ArgumentNullException.ThrowIfNull(tradingRulesProvider);
        ArgumentNullException.ThrowIfNull(latestPriceProvider);
        ArgumentNullException.ThrowIfNull(orderIdRegistry);
        ArgumentNullException.ThrowIfNull(tradingRulesEngine);
        ArgumentNullException.ThrowIfNull(orderRepository);

        _tradingRulesProvider = tradingRulesProvider;
        _latestPriceProvider = latestPriceProvider;
        _orderIdRegistry = orderIdRegistry;
        _tradingRulesEngine = tradingRulesEngine;
        _orderRepository = orderRepository;
    }

    public async Task<TradeDecision> ProcessAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        TradingRulesConfiguration tradingRules = _tradingRulesProvider.Current;
        bool newlyRegistered = await _orderIdRegistry.TryRegisterAsync(
            order.Id,
            cancellationToken);
        MarketPrice? marketPrice = _latestPriceProvider.GetLatest(order.Symbol);

        var rejectionReasons = new List<RejectionReason>();

        if (tradingRules.DuplicateOrderIdCheckEnabled && !newlyRegistered)
        {
            rejectionReasons.Add(
                new RejectionReason(
                    "DuplicateOrderId",
                    $"Order ID '{order.Id}' is already registered."));
        }

        if (marketPrice is null)
        {
            rejectionReasons.Add(
                new RejectionReason(
                    "MarketPriceUnavailable",
                    $"No current market price is available for symbol '{order.Symbol}'."));
        }
        else
        {
            TradeValidationResult validationResult = _tradingRulesEngine.Evaluate(
                order,
                marketPrice,
                tradingRules);

            rejectionReasons.AddRange(validationResult.RejectionReasons);
        }

        TradeDecision decision = rejectionReasons.Count == 0
            ? TradeDecision.Accepted(order.Id)
            : TradeDecision.Rejected(order.Id, rejectionReasons.ToArray());

        await _orderRepository.SaveAsync(order, decision, cancellationToken);

        return decision;
    }
}
