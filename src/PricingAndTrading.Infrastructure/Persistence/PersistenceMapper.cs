using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence.Entities;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure.Persistence;

internal static class PersistenceMapper
{
    public static OrderEntity ToEntity(
        Order order,
        TradeDecision decision,
        Guid persistenceId)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(decision);

        var entity = new OrderEntity
        {
            PersistenceId = persistenceId,
            OrderId = order.Id,
            Symbol = order.Symbol,
            Side = order.Side,
            Type = order.Type,
            Price = order.Price,
            Quantity = order.Quantity,
            Source = order.Source,
            CreatedAt = order.CreatedAt.ToUniversalTime(),
            Status = decision.Status
        };

        for (var sequence = 0; sequence < decision.RejectionReasons.Count; sequence++)
        {
            RejectionReason reason = decision.RejectionReasons[sequence];
            entity.RejectionReasons.Add(new OrderRejectionReasonEntity
            {
                OrderPersistenceId = persistenceId,
                Sequence = sequence,
                Code = reason.Code,
                Message = reason.Message,
                Order = entity
            });
        }

        return entity;
    }

    public static TradingRulesEntity ToEntity(
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(tradingRules);

        var entity = new TradingRulesEntity
        {
            Id = TradingRulesEntity.ActiveConfigurationId
        };

        Update(entity, tradingRules);
        return entity;
    }

    public static void Update(
        TradingRulesEntity entity,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(tradingRules);

        entity.MaximumNotionalAmount = tradingRules.MaximumNotionalAmount;
        entity.MaximumQuantity = tradingRules.MaximumQuantity;
        entity.MaximumPriceDeviationPercent =
            tradingRules.MaximumPriceDeviationPercent;
        entity.DuplicateOrderIdCheckEnabled =
            tradingRules.DuplicateOrderIdCheckEnabled;
        entity.SymbolWhitelistEnabled = tradingRules.SymbolWhitelistEnabled;
        entity.SymbolWhitelist = tradingRules.SymbolWhitelist.ToArray();
        entity.AutoTradingSpreadThresholdPercent =
            tradingRules.AutoTradingSpreadThresholdPercent;
    }

    public static TradingRulesConfiguration ToDomain(TradingRulesEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TradingRulesConfiguration(
            entity.MaximumNotionalAmount,
            entity.MaximumQuantity,
            entity.DuplicateOrderIdCheckEnabled,
            entity.SymbolWhitelistEnabled,
            entity.SymbolWhitelist,
            entity.AutoTradingSpreadThresholdPercent,
            entity.MaximumPriceDeviationPercent);
    }

    public static LatestPriceEntity ToEntity(MarketPrice marketPrice)
    {
        ArgumentNullException.ThrowIfNull(marketPrice);

        var entity = new LatestPriceEntity();
        Update(entity, marketPrice);
        return entity;
    }

    public static void Update(
        LatestPriceEntity entity,
        MarketPrice marketPrice)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(marketPrice);

        entity.Symbol = marketPrice.Symbol;
        entity.BidPrice = marketPrice.BidPrice;
        entity.AskPrice = marketPrice.AskPrice;
        entity.CurrentMarketPrice = marketPrice.CurrentMarketPrice;
        entity.Spread = marketPrice.Spread;
        entity.SpreadPercent = marketPrice.SpreadPercent;
        entity.Timestamp = marketPrice.Timestamp.ToUniversalTime();
    }

    public static MarketPrice ToDomain(LatestPriceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return MarketPrice.From(
            new PriceTick(
                entity.Symbol,
                entity.BidPrice,
                entity.AskPrice,
                entity.Timestamp));
    }
}
