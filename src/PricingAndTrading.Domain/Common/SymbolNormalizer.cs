namespace PricingAndTrading.Domain.Common;

internal static class SymbolNormalizer
{
    public static string Normalize(string? symbol, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol, parameterName);

        return symbol.Trim().ToUpperInvariant();
    }
}
