using PricingAndTrading.Application.AutoTrading;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.Pricing;

public sealed class PriceProcessorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_ValidPriceTick_UpdatesStateAndEvaluatesAutoTradingWithOneSnapshot()
    {
        var context = new PriceProcessorTestContext();
        MarketPrice previous = CreateMarketPrice(98m, 100m);
        context.LatestPriceStore.PreviousPrice = previous;
        PriceProcessor processor = context.CreateProcessor();

        await processor.ProcessAsync(context.PriceTick);

        MarketPrice current = Assert.IsType<MarketPrice>(
            context.LatestPriceStore.UpdatedPrice);
        Assert.Equal(1, context.LatestPriceStore.UpdateCount);
        Assert.Equal(context.PriceTick.Symbol, current.Symbol);
        Assert.Equal(context.PriceTick.BidPrice, current.BidPrice);
        Assert.Equal(context.PriceTick.AskPrice, current.AskPrice);
        Assert.Equal(context.PriceTick.Timestamp, current.Timestamp);
        Assert.Equal(100m, current.CurrentMarketPrice);
        Assert.Equal(2m, current.Spread);
        Assert.Equal(2m, current.SpreadPercent);
        Assert.Equal(1, context.TradingRulesProvider.ReadCount);
        Assert.Equal(1, context.AutoTradingEngine.CallCount);
        Assert.Same(previous, context.AutoTradingEngine.Previous);
        Assert.Same(current, context.AutoTradingEngine.Current);
        Assert.Same(context.TradingRules, context.AutoTradingEngine.TradingRules);
        Assert.Equal(["update", "evaluate"], context.Events);
        Assert.Equal(0, context.OrderProcessor.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_TwoPriceTicks_EvaluatesAutoTradingOncePerTick()
    {
        var context = new PriceProcessorTestContext();
        PriceProcessor processor = context.CreateProcessor();

        await processor.ProcessAsync(context.PriceTick);
        await processor.ProcessAsync(context.PriceTick);

        Assert.Equal(2, context.LatestPriceStore.UpdateCount);
        Assert.Equal(2, context.TradingRulesProvider.ReadCount);
        Assert.Equal(2, context.AutoTradingEngine.CallCount);
        Assert.Equal(0, context.OrderProcessor.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_AutoOrderGenerated_SubmitsSameOrderOnceWithCancellationToken()
    {
        var context = new PriceProcessorTestContext();
        Order expectedOrder = CreateOrder();
        context.AutoTradingEngine.OrderToReturn = expectedOrder;
        PriceProcessor processor = context.CreateProcessor();
        using var cancellationSource = new CancellationTokenSource();

        await processor.ProcessAsync(context.PriceTick, cancellationSource.Token);

        Assert.Equal(1, context.OrderProcessor.CallCount);
        Assert.Same(expectedOrder, context.OrderProcessor.Order);
        Assert.Equal(
            cancellationSource.Token,
            context.OrderProcessor.CancellationToken);
    }

    [Fact]
    public async Task ProcessAsync_OrderProcessorThrows_PropagatesException()
    {
        var context = new PriceProcessorTestContext();
        context.AutoTradingEngine.OrderToReturn = CreateOrder();
        var expectedException = new InvalidOperationException("Order processing failed.");
        context.OrderProcessor.ExceptionToThrow = expectedException;
        PriceProcessor processor = context.CreateProcessor();

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                processor.ProcessAsync(context.PriceTick).AsTask());

        Assert.Same(expectedException, actualException);
        Assert.Equal(1, context.OrderProcessor.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_OrderProcessingIsCancelled_PropagatesCancellation()
    {
        var context = new PriceProcessorTestContext();
        context.AutoTradingEngine.OrderToReturn = CreateOrder();
        PriceProcessor processor = context.CreateProcessor();
        using var cancellationSource = new CancellationTokenSource();
        context.OrderProcessor.OnProcess = cancellationSource.Cancel;
        context.OrderProcessor.ObserveCancellation = true;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(
                context.PriceTick,
                cancellationSource.Token).AsTask());

        Assert.Equal(1, context.OrderProcessor.CallCount);
        Assert.Equal(
            cancellationSource.Token,
            context.OrderProcessor.CancellationToken);
    }

    [Fact]
    public async Task ProcessAsync_NullPriceTick_ThrowsArgumentNullException()
    {
        var context = new PriceProcessorTestContext();
        PriceProcessor processor = context.CreateProcessor();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            processor.ProcessAsync(null!).AsTask());

        Assert.Equal(0, context.LatestPriceStore.UpdateCount);
        Assert.Equal(0, context.AutoTradingEngine.CallCount);
        Assert.Equal(0, context.OrderProcessor.CallCount);
    }

    private static MarketPrice CreateMarketPrice(decimal bidPrice, decimal askPrice) =>
        MarketPrice.From(new PriceTick("EURUSD", bidPrice, askPrice, Timestamp));

    private static Order CreateOrder() =>
        new(
            Guid.Parse("1bc8e054-b471-447a-b47d-568976769fd4"),
            "EURUSD",
            OrderSide.Sell,
            OrderType.Limit,
            100m,
            5m,
            OrderSource.AutoGenerated,
            Timestamp);

    private sealed class PriceProcessorTestContext
    {
        public PriceProcessorTestContext()
        {
            PriceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);
            TradingRules = new TradingRulesConfiguration(
                maximumNotionalAmount: 100_000m,
                maximumQuantity: 1_000m,
                duplicateOrderIdCheckEnabled: true,
                symbolWhitelistEnabled: false,
                symbolWhitelist: null,
                autoTradingSpreadThresholdPercent: 1m);
            Events = [];
            LatestPriceStore = new RecordingLatestPriceStore(Events);
            TradingRulesProvider = new RecordingTradingRulesProvider(TradingRules);
            AutoTradingEngine = new RecordingAutoTradingEngine(Events);
            OrderProcessor = new RecordingOrderProcessor();
        }

        public PriceTick PriceTick { get; }

        public TradingRulesConfiguration TradingRules { get; }

        public List<string> Events { get; }

        public RecordingLatestPriceStore LatestPriceStore { get; }

        public RecordingTradingRulesProvider TradingRulesProvider { get; }

        public RecordingAutoTradingEngine AutoTradingEngine { get; }

        public RecordingOrderProcessor OrderProcessor { get; }

        public PriceProcessor CreateProcessor() =>
            new(
                LatestPriceStore,
                TradingRulesProvider,
                AutoTradingEngine,
                OrderProcessor);
    }

    private sealed class RecordingLatestPriceStore : ILatestPriceStore
    {
        private readonly List<string> _events;

        public RecordingLatestPriceStore(List<string> events)
        {
            _events = events;
        }

        public MarketPrice? PreviousPrice { get; set; }

        public MarketPrice? UpdatedPrice { get; private set; }

        public int UpdateCount { get; private set; }

        public MarketPrice? GetLatest(string symbol) => UpdatedPrice;

        public MarketPrice? Update(MarketPrice marketPrice)
        {
            _events.Add("update");
            UpdateCount++;
            UpdatedPrice = marketPrice;
            return PreviousPrice;
        }
    }

    private sealed class RecordingTradingRulesProvider : ITradingRulesProvider
    {
        private readonly TradingRulesConfiguration _current;

        public RecordingTradingRulesProvider(TradingRulesConfiguration current)
        {
            _current = current;
        }

        public int ReadCount { get; private set; }

        public TradingRulesConfiguration Current
        {
            get
            {
                ReadCount++;
                return _current;
            }
        }
    }

    private sealed class RecordingAutoTradingEngine : IAutoTradingEngine
    {
        private readonly List<string> _events;

        public RecordingAutoTradingEngine(List<string> events)
        {
            _events = events;
        }

        public Order? OrderToReturn { get; set; }

        public int CallCount { get; private set; }

        public MarketPrice? Previous { get; private set; }

        public MarketPrice? Current { get; private set; }

        public TradingRulesConfiguration? TradingRules { get; private set; }

        public Order? Evaluate(
            MarketPrice? previous,
            MarketPrice current,
            TradingRulesConfiguration tradingRules)
        {
            _events.Add("evaluate");
            CallCount++;
            Previous = previous;
            Current = current;
            TradingRules = tradingRules;
            return OrderToReturn;
        }
    }

    private sealed class RecordingOrderProcessor : IOrderProcessor
    {
        public int CallCount { get; private set; }

        public Order? Order { get; private set; }

        public CancellationToken? CancellationToken { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Action? OnProcess { get; set; }

        public bool ObserveCancellation { get; set; }

        public Task<TradeDecision> ProcessAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Order = order;
            CancellationToken = cancellationToken;
            OnProcess?.Invoke();

            if (ObserveCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(TradeDecision.Accepted(order.Id));
        }
    }
}
