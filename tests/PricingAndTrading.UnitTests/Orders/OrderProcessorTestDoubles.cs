using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.Orders;

internal sealed class OrderProcessorTestContext
{
    private static readonly Guid OrderId =
        Guid.Parse("b2c78f51-d3ee-460e-897f-50605e6e918d");

    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 9, 0, 0, TimeSpan.Zero);

    public OrderProcessorTestContext()
    {
        Order = new Order(
            OrderId,
            "EURUSD",
            OrderSide.Buy,
            OrderType.Limit,
            100m,
            5m,
            OrderSource.Api,
            Timestamp);

        MarketPrice = MarketPrice.From(
            new PriceTick("EURUSD", 99m, 101m, Timestamp));

        TradingRulesProvider = new StubTradingRulesProvider(
            CreateTradingRules(duplicateOrderIdCheckEnabled: true));
        LatestPriceProvider = new StubLatestPriceProvider(MarketPrice);
        OrderIdRegistry = new StubOrderIdRegistry();
        TradingRulesEngine = new StubTradingRulesEngine(
            TradeValidationResult.Valid());
        OrderRepository = new SpyOrderRepository();
    }

    public Order Order { get; }

    public MarketPrice MarketPrice { get; }

    public StubTradingRulesProvider TradingRulesProvider { get; }

    public StubLatestPriceProvider LatestPriceProvider { get; }

    public StubOrderIdRegistry OrderIdRegistry { get; }

    public StubTradingRulesEngine TradingRulesEngine { get; }

    public SpyOrderRepository OrderRepository { get; }

    public OrderProcessor CreateProcessor()
    {
        return new OrderProcessor(
            TradingRulesProvider,
            LatestPriceProvider,
            OrderIdRegistry,
            TradingRulesEngine,
            OrderRepository);
    }

    public static TradingRulesConfiguration CreateTradingRules(
        bool duplicateOrderIdCheckEnabled)
    {
        return new TradingRulesConfiguration(
            maximumNotionalAmount: 1_000m,
            maximumQuantity: 10m,
            duplicateOrderIdCheckEnabled,
            symbolWhitelistEnabled: false,
            symbolWhitelist: null,
            autoTradingSpreadThresholdPercent: 0.25m,
            maximumPriceDeviationPercent: 0.8m);
    }
}

internal sealed class StubTradingRulesProvider : ITradingRulesProvider
{
    public StubTradingRulesProvider(TradingRulesConfiguration current)
    {
        CurrentValue = current;
    }

    public TradingRulesConfiguration CurrentValue { get; set; }

    public int ReadCount { get; private set; }

    public TradingRulesConfiguration Current
    {
        get
        {
            ReadCount++;
            return CurrentValue;
        }
    }
}

internal sealed class StubLatestPriceProvider : ILatestPriceProvider
{
    public StubLatestPriceProvider(MarketPrice? latestPrice)
    {
        LatestPrice = latestPrice;
    }

    public MarketPrice? LatestPrice { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public string? LastSymbol { get; private set; }

    public MarketPrice? GetLatest(string symbol)
    {
        CallCount++;
        LastSymbol = symbol;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return LatestPrice;
    }
}

internal sealed class StubOrderIdRegistry : IOrderIdRegistry
{
    private readonly HashSet<Guid> _registeredOrderIds = [];

    public bool? TryRegisterResult { get; set; }

    public bool ObserveCancellation { get; set; }

    public int CallCount { get; private set; }

    public Guid? LastOrderId { get; private set; }

    public CancellationToken? LastCancellationToken { get; private set; }

    public ValueTask<bool> TryRegisterAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastOrderId = orderId;
        LastCancellationToken = cancellationToken;

        if (ObserveCancellation)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        bool newlyRegistered = TryRegisterResult
            ?? _registeredOrderIds.Add(orderId);

        return ValueTask.FromResult(newlyRegistered);
    }
}

internal sealed class StubTradingRulesEngine : ITradingRulesEngine
{
    public StubTradingRulesEngine(TradeValidationResult result)
    {
        Result = result;
    }

    public TradeValidationResult Result { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public Order? LastOrder { get; private set; }

    public MarketPrice? LastMarketPrice { get; private set; }

    public TradingRulesConfiguration? LastTradingRules { get; private set; }

    public TradeValidationResult Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        CallCount++;
        LastOrder = order;
        LastMarketPrice = marketPrice;
        LastTradingRules = tradingRules;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Result;
    }
}

internal sealed class SpyOrderRepository : IOrderRepository
{
    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public Order? LastOrder { get; private set; }

    public TradeDecision? LastDecision { get; private set; }

    public CancellationToken? LastCancellationToken { get; private set; }

    public Task SaveAsync(
        Order order,
        TradeDecision decision,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastOrder = order;
        LastDecision = decision;
        LastCancellationToken = cancellationToken;

        return ExceptionToThrow is null
            ? Task.CompletedTask
            : Task.FromException(ExceptionToThrow);
    }
}
