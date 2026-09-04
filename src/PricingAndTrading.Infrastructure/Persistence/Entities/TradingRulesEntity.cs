namespace PricingAndTrading.Infrastructure.Persistence.Entities;

internal sealed class TradingRulesEntity
{
    public const int ActiveConfigurationId = 1;

    public int Id { get; set; }

    public decimal MaximumNotionalAmount { get; set; }

    public decimal MaximumQuantity { get; set; }

    public decimal MaximumPriceDeviationPercent { get; set; }

    public bool DuplicateOrderIdCheckEnabled { get; set; }

    public bool SymbolWhitelistEnabled { get; set; }

    public string[] SymbolWhitelist { get; set; } = [];

    public decimal AutoTradingSpreadThresholdPercent { get; set; }
}
