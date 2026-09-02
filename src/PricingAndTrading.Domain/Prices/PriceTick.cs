using PricingAndTrading.Domain.Common;

namespace PricingAndTrading.Domain.Prices;

public sealed class PriceTick
{
    public PriceTick(
        string symbol,
        decimal bidPrice,
        decimal askPrice,
        DateTimeOffset timestamp)
    {
        Symbol = SymbolNormalizer.Normalize(symbol, nameof(symbol));

        if (bidPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bidPrice),
                bidPrice,
                "Bid price must be greater than zero.");
        }

        if (askPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(askPrice),
                askPrice,
                "Ask price must be greater than zero.");
        }

        if (bidPrice >= askPrice)
        {
            throw new ArgumentException(
                "Bid price must be strictly lower than ask price.",
                nameof(bidPrice));
        }

        BidPrice = bidPrice;
        AskPrice = askPrice;
        Timestamp = timestamp;
    }

    public string Symbol { get; }

    public decimal BidPrice { get; }

    public decimal AskPrice { get; }

    public DateTimeOffset Timestamp { get; }
}
