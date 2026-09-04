using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.Api;

internal sealed class RecordingOrderProcessor : IOrderProcessor
{
    public Exception? ExceptionToThrow { get; set; }

    public Func<Order, TradeDecision> DecisionFactory { get; set; } =
        order => TradeDecision.Accepted(order.Id);

    public int CallCount { get; private set; }

    public Order? LastOrder { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<TradeDecision> ProcessAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastOrder = order;
        LastCancellationToken = cancellationToken;

        return ExceptionToThrow is null
            ? Task.FromResult(DecisionFactory(order))
            : Task.FromException<TradeDecision>(ExceptionToThrow);
    }
}

internal sealed class RecordingOrderHistoryRepository : IOrderHistoryRepository
{
    public OrderHistoryPage Result { get; set; } =
        new([], OrderHistoryQuery.DefaultPage, OrderHistoryQuery.DefaultPageSize, 0);

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public OrderHistoryQuery? LastQuery { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<OrderHistoryPage> GetAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastQuery = query;
        LastCancellationToken = cancellationToken;

        return ExceptionToThrow is null
            ? Task.FromResult(Result)
            : Task.FromException<OrderHistoryPage>(ExceptionToThrow);
    }
}

internal sealed class StubTradingRulesProvider(
    TradingRulesConfiguration current) : ITradingRulesProvider
{
    public TradingRulesConfiguration Current { get; } = current;
}

internal sealed class RecordingTradingRulesUpdater : ITradingRulesUpdater
{
    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public TradingRulesConfiguration? LastRules { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task UpdateAsync(
        TradingRulesConfiguration tradingRules,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastRules = tradingRules;
        LastCancellationToken = cancellationToken;

        return ExceptionToThrow is null
            ? Task.CompletedTask
            : Task.FromException(ExceptionToThrow);
    }
}

internal sealed class StubLatestPriceProvider(MarketPrice? marketPrice) :
    ILatestPriceProvider
{
    public int CallCount { get; private set; }

    public string? LastSymbol { get; private set; }

    public MarketPrice? GetLatest(string symbol)
    {
        CallCount++;
        LastSymbol = symbol;
        return marketPrice;
    }
}
