namespace PricingAndTrading.Api.Contracts.Responses;

public sealed record GetTradingRulesResponse(
    decimal MaximumNotionalAmount,
    decimal MaximumQuantity,
    decimal MaximumPriceDeviationPercent,
    bool DuplicateOrderIdCheckEnabled,
    bool SymbolWhitelistEnabled,
    IReadOnlyList<string> SymbolWhitelist,
    decimal AutoTradingSpreadThresholdPercent);
