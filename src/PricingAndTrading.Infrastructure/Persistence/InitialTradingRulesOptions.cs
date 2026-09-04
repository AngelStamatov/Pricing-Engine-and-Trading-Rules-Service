using Microsoft.Extensions.Configuration;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure.Persistence;

internal sealed class InitialTradingRulesOptions
{
    private const string SectionName = "TradingRules";

    public decimal? MaximumNotionalAmount { get; init; }

    public decimal? MaximumQuantity { get; init; }

    public decimal? MaximumPriceDeviationPercent { get; init; }

    public bool? DuplicateOrderIdCheckEnabled { get; init; }

    public bool? SymbolWhitelistEnabled { get; init; }

    public string[]? SymbolWhitelist { get; init; }

    public decimal? AutoTradingSpreadThresholdPercent { get; init; }

    public static TradingRulesConfiguration CreateDomainConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        InitialTradingRulesOptions options = configuration
            .GetRequiredSection(SectionName)
            .Get<InitialTradingRulesOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{SectionName}' is required.");

        return options.ToDomain();
    }

    internal TradingRulesConfiguration ToDomain()
    {
        decimal maximumNotionalAmount = Require(
            MaximumNotionalAmount,
            nameof(MaximumNotionalAmount));
        decimal maximumQuantity = Require(
            MaximumQuantity,
            nameof(MaximumQuantity));
        decimal maximumPriceDeviationPercent = Require(
            MaximumPriceDeviationPercent,
            nameof(MaximumPriceDeviationPercent));
        bool duplicateOrderIdCheckEnabled = Require(
            DuplicateOrderIdCheckEnabled,
            nameof(DuplicateOrderIdCheckEnabled));
        bool symbolWhitelistEnabled = Require(
            SymbolWhitelistEnabled,
            nameof(SymbolWhitelistEnabled));
        decimal autoTradingSpreadThresholdPercent = Require(
            AutoTradingSpreadThresholdPercent,
            nameof(AutoTradingSpreadThresholdPercent));

        if (SymbolWhitelist is null)
        {
            throw Missing(nameof(SymbolWhitelist));
        }

        return new TradingRulesConfiguration(
            maximumNotionalAmount,
            maximumQuantity,
            duplicateOrderIdCheckEnabled,
            symbolWhitelistEnabled,
            SymbolWhitelist,
            autoTradingSpreadThresholdPercent,
            maximumPriceDeviationPercent);
    }

    private static T Require<T>(T? value, string propertyName)
        where T : struct =>
        value ?? throw Missing(propertyName);

    private static InvalidOperationException Missing(string propertyName) =>
        new($"Configuration value '{SectionName}:{propertyName}' is required.");
}
