namespace PricingAndTrading.Domain.Prices;

public sealed class MarketPrice
{
    private MarketPrice(PriceTick priceTick)
    {
        Symbol = priceTick.Symbol;
        BidPrice = priceTick.BidPrice;
        AskPrice = priceTick.AskPrice;
        CurrentMarketPrice = (BidPrice + AskPrice) / 2m;
        Spread = AskPrice - BidPrice;
        SpreadPercent = (Spread / CurrentMarketPrice) * 100m;
        Timestamp = priceTick.Timestamp;
    }

    public string Symbol { get; }

    public decimal BidPrice { get; }

    public decimal AskPrice { get; }

    public decimal CurrentMarketPrice { get; }

    public decimal Spread { get; }

    public decimal SpreadPercent { get; }

    public DateTimeOffset Timestamp { get; }

    public static MarketPrice From(PriceTick priceTick)
    {
        ArgumentNullException.ThrowIfNull(priceTick);

        return new MarketPrice(priceTick);
    }
}
