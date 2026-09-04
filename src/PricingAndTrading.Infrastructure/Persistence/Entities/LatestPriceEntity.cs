namespace PricingAndTrading.Infrastructure.Persistence.Entities;

internal sealed class LatestPriceEntity
{
    public string Symbol { get; set; } = string.Empty;

    public decimal BidPrice { get; set; }

    public decimal AskPrice { get; set; }

    public decimal CurrentMarketPrice { get; set; }

    public decimal Spread { get; set; }

    public decimal SpreadPercent { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
