using PricingAndTrading.Domain.Common;

namespace PricingAndTrading.Domain.TradingRules;

public sealed class TradingRules
{
    private const decimal DefaultMaximumPriceDeviationPercent = 0.8m;

    public TradingRules(
        decimal maximumNotionalAmount,
        decimal maximumQuantity,
        bool duplicateOrderIdCheckEnabled,
        bool symbolWhitelistEnabled,
        IEnumerable<string>? symbolWhitelist,
        decimal autoTradingSpreadThresholdPercent,
        decimal maximumPriceDeviationPercent = DefaultMaximumPriceDeviationPercent)
    {
        if (maximumNotionalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumNotionalAmount),
                maximumNotionalAmount,
                "Maximum notional amount must be greater than zero.");
        }

        if (maximumQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumQuantity),
                maximumQuantity,
                "Maximum quantity must be greater than zero.");
        }

        if (maximumPriceDeviationPercent < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPriceDeviationPercent),
                maximumPriceDeviationPercent,
                "Maximum price deviation percent must not be negative.");
        }

        if (autoTradingSpreadThresholdPercent < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(autoTradingSpreadThresholdPercent),
                autoTradingSpreadThresholdPercent,
                "Auto-trading spread threshold percent must not be negative.");
        }

        IReadOnlyList<string> normalizedWhitelist = NormalizeWhitelist(symbolWhitelist);

        if (symbolWhitelistEnabled && normalizedWhitelist.Count == 0)
        {
            throw new ArgumentException(
                "An enabled symbol whitelist must contain at least one valid symbol.",
                nameof(symbolWhitelist));
        }

        MaximumNotionalAmount = maximumNotionalAmount;
        MaximumQuantity = maximumQuantity;
        MaximumPriceDeviationPercent = maximumPriceDeviationPercent;
        DuplicateOrderIdCheckEnabled = duplicateOrderIdCheckEnabled;
        SymbolWhitelistEnabled = symbolWhitelistEnabled;
        SymbolWhitelist = normalizedWhitelist;
        AutoTradingSpreadThresholdPercent = autoTradingSpreadThresholdPercent;
    }

    public decimal MaximumNotionalAmount { get; }

    public decimal MaximumQuantity { get; }

    public decimal MaximumPriceDeviationPercent { get; }

    public bool DuplicateOrderIdCheckEnabled { get; }

    public bool SymbolWhitelistEnabled { get; }

    public IReadOnlyList<string> SymbolWhitelist { get; }

    public decimal AutoTradingSpreadThresholdPercent { get; }

    private static IReadOnlyList<string> NormalizeWhitelist(
        IEnumerable<string>? symbolWhitelist)
    {
        if (symbolWhitelist is null)
        {
            return Array.Empty<string>();
        }

        var normalizedSymbols = new HashSet<string>(StringComparer.Ordinal);

        foreach (string symbol in symbolWhitelist)
        {
            normalizedSymbols.Add(
                SymbolNormalizer.Normalize(symbol, nameof(symbolWhitelist)));
        }

        string[] orderedSymbols = normalizedSymbols
            .OrderBy(static symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        return Array.AsReadOnly(orderedSymbols);
    }
}
