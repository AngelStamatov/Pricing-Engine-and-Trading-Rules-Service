namespace PricingAndTrading.Api.Contracts.Responses;

public sealed record LatestPriceResponse(
    string Symbol,
    decimal BidPrice,
    decimal AskPrice,
    decimal CurrentMarketPrice,
    decimal Spread,
    decimal SpreadPercent,
    DateTimeOffset Timestamp);
