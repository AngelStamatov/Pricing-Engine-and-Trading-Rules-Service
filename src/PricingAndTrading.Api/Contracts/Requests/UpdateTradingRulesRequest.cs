namespace PricingAndTrading.Api.Contracts.Requests;

public sealed class UpdateTradingRulesRequest
{
    public required decimal MaximumNotionalAmount { get; init; }

    public required decimal MaximumQuantity { get; init; }

    public required decimal MaximumPriceDeviationPercent { get; init; }

    public required bool DuplicateOrderIdCheckEnabled { get; init; }

    public required bool SymbolWhitelistEnabled { get; init; }

    public required IReadOnlyCollection<string>? SymbolWhitelist { get; init; }

    public required decimal AutoTradingSpreadThresholdPercent { get; init; }
}
