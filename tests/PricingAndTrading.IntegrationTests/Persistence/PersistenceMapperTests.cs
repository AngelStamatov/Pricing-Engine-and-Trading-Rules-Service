using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Entities;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class PersistenceMapperTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToEntity_AcceptedOrder_MapsOrderAndDecision()
    {
        Order order = CreateOrder(Guid.NewGuid());
        TradeDecision decision = TradeDecision.Accepted(order.Id);
        Guid persistenceId = Guid.NewGuid();

        OrderEntity entity = PersistenceMapper.ToEntity(
            order,
            decision,
            persistenceId);

        Assert.Equal(persistenceId, entity.PersistenceId);
        Assert.Equal(order.Id, entity.OrderId);
        Assert.Equal(order.Symbol, entity.Symbol);
        Assert.Equal(order.Side, entity.Side);
        Assert.Equal(order.Type, entity.Type);
        Assert.Equal(order.Price, entity.Price);
        Assert.Equal(order.Quantity, entity.Quantity);
        Assert.Equal(order.Source, entity.Source);
        Assert.Equal(order.CreatedAt, entity.CreatedAt);
        Assert.Equal(OrderStatus.Accepted, entity.Status);
        Assert.Empty(entity.RejectionReasons);
    }

    [Fact]
    public void ToEntity_RejectedOrder_MapsStructuredReasonsInOriginalOrder()
    {
        Order order = CreateOrder(Guid.NewGuid());
        var first = new RejectionReason("First", "First reason");
        var second = new RejectionReason("Second", "Second reason");
        TradeDecision decision = TradeDecision.Rejected(order.Id, first, second);

        OrderEntity entity = PersistenceMapper.ToEntity(
            order,
            decision,
            Guid.NewGuid());

        Assert.Equal(OrderStatus.Rejected, entity.Status);
        Assert.Collection(
            entity.RejectionReasons.OrderBy(reason => reason.Sequence),
            reason =>
            {
                Assert.Equal(0, reason.Sequence);
                Assert.Equal(first.Code, reason.Code);
                Assert.Equal(first.Message, reason.Message);
            },
            reason =>
            {
                Assert.Equal(1, reason.Sequence);
                Assert.Equal(second.Code, reason.Code);
                Assert.Equal(second.Message, reason.Message);
            });
    }

    [Fact]
    public void ToEntity_RepeatedBusinessOrderId_UsesIndependentPersistenceIds()
    {
        Guid orderId = Guid.NewGuid();
        Order firstOrder = CreateOrder(orderId);
        Order secondOrder = CreateOrder(orderId);
        Guid firstPersistenceId = Guid.NewGuid();
        Guid secondPersistenceId = Guid.NewGuid();

        OrderEntity first = PersistenceMapper.ToEntity(
            firstOrder,
            TradeDecision.Accepted(orderId),
            firstPersistenceId);
        OrderEntity second = PersistenceMapper.ToEntity(
            secondOrder,
            TradeDecision.Accepted(orderId),
            secondPersistenceId);

        Assert.Equal(first.OrderId, second.OrderId);
        Assert.NotEqual(first.PersistenceId, second.PersistenceId);
    }

    [Fact]
    public void TradingRulesMapping_ValidSnapshot_RoundTripsAllValues()
    {
        var rules = new TradingRulesConfiguration(
            maximumNotionalAmount: 100_000m,
            maximumQuantity: 10_000m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: true,
            symbolWhitelist: ["EURUSD", "GBPUSD"],
            autoTradingSpreadThresholdPercent: 0.02m,
            maximumPriceDeviationPercent: 0.8m);

        TradingRulesEntity entity = PersistenceMapper.ToEntity(rules);
        TradingRulesConfiguration restored = PersistenceMapper.ToDomain(entity);

        Assert.Equal(rules.MaximumNotionalAmount, restored.MaximumNotionalAmount);
        Assert.Equal(rules.MaximumQuantity, restored.MaximumQuantity);
        Assert.Equal(
            rules.MaximumPriceDeviationPercent,
            restored.MaximumPriceDeviationPercent);
        Assert.Equal(
            rules.DuplicateOrderIdCheckEnabled,
            restored.DuplicateOrderIdCheckEnabled);
        Assert.Equal(rules.SymbolWhitelistEnabled, restored.SymbolWhitelistEnabled);
        Assert.Equal(rules.SymbolWhitelist, restored.SymbolWhitelist);
        Assert.Equal(
            rules.AutoTradingSpreadThresholdPercent,
            restored.AutoTradingSpreadThresholdPercent);
    }

    [Fact]
    public void LatestPriceMapping_ValidMarketPrice_RoundTripsAllValues()
    {
        MarketPrice price = MarketPrice.From(
            new PriceTick("EURUSD", 99m, 101m, Timestamp));

        LatestPriceEntity entity = PersistenceMapper.ToEntity(price);
        MarketPrice restored = PersistenceMapper.ToDomain(entity);

        Assert.Equal(price.Symbol, restored.Symbol);
        Assert.Equal(price.BidPrice, restored.BidPrice);
        Assert.Equal(price.AskPrice, restored.AskPrice);
        Assert.Equal(price.CurrentMarketPrice, restored.CurrentMarketPrice);
        Assert.Equal(price.Spread, restored.Spread);
        Assert.Equal(price.SpreadPercent, restored.SpreadPercent);
        Assert.Equal(price.Timestamp, restored.Timestamp);
    }

    [Fact]
    public void LatestPriceMapping_InvalidPersistedPrices_ThrowsDomainInvariantException()
    {
        MarketPrice price = MarketPrice.From(
            new PriceTick("EURUSD", 99m, 101m, Timestamp));
        LatestPriceEntity entity = PersistenceMapper.ToEntity(price);
        entity.BidPrice = 0m;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PersistenceMapper.ToDomain(entity));
    }

    private static Order CreateOrder(Guid orderId) =>
        new(
            orderId,
            "EURUSD",
            OrderSide.Buy,
            OrderType.Limit,
            100m,
            5m,
            OrderSource.Api,
            Timestamp);
}
