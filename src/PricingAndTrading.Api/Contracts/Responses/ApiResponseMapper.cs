using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Api.Contracts.Responses;

internal static class ApiResponseMapper
{
    public static SubmitOrderResponse ToResponse(TradeDecision decision) =>
        new(
            decision.OrderId,
            decision.Status,
            decision.RejectionReasons
                .Select(reason => new RejectionReasonResponse(
                    reason.Code,
                    reason.Message))
                .ToArray());

    public static GetTradingRulesResponse ToResponse(
        TradingRulesConfiguration tradingRules) =>
        new(
            tradingRules.MaximumNotionalAmount,
            tradingRules.MaximumQuantity,
            tradingRules.MaximumPriceDeviationPercent,
            tradingRules.DuplicateOrderIdCheckEnabled,
            tradingRules.SymbolWhitelistEnabled,
            tradingRules.SymbolWhitelist.ToArray(),
            tradingRules.AutoTradingSpreadThresholdPercent);

    public static LatestPriceResponse ToResponse(MarketPrice marketPrice) =>
        new(
            marketPrice.Symbol,
            marketPrice.BidPrice,
            marketPrice.AskPrice,
            marketPrice.CurrentMarketPrice,
            marketPrice.Spread,
            marketPrice.SpreadPercent,
            marketPrice.Timestamp);

    public static OrderHistoryPageResponse ToResponse(OrderHistoryPage page) =>
        new(
            page.Items.Select(ToResponse).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount);

    private static OrderHistoryItemResponse ToResponse(OrderHistoryItem item) =>
        new(
            item.OrderId,
            item.Symbol,
            item.Side,
            item.Type,
            item.Price,
            item.Quantity,
            item.Source,
            item.CreatedAt,
            item.Status,
            item.RejectionReasons
                .Select(reason => new RejectionReasonResponse(
                    reason.Code,
                    reason.Message))
                .ToArray());
}
