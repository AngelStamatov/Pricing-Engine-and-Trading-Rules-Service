using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Api.Controllers;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.IntegrationTests.Api;

public sealed class PricesControllerTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 4, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Get_KnownPrice_ReturnsOkWithEveryValue()
    {
        MarketPrice price = MarketPrice.From(
            new PriceTick("EURUSD", 99m, 101m, Timestamp));
        var provider = new StubLatestPriceProvider(price);
        var controller = new PricesController(provider);

        ActionResult<LatestPriceResponse> result = controller.Get(" eurusd ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        LatestPriceResponse response = Assert.IsType<LatestPriceResponse>(ok.Value);
        Assert.Equal(price.Symbol, response.Symbol);
        Assert.Equal(price.BidPrice, response.BidPrice);
        Assert.Equal(price.AskPrice, response.AskPrice);
        Assert.Equal(price.CurrentMarketPrice, response.CurrentMarketPrice);
        Assert.Equal(price.Spread, response.Spread);
        Assert.Equal(price.SpreadPercent, response.SpreadPercent);
        Assert.Equal(price.Timestamp, response.Timestamp);
        Assert.Equal(" eurusd ", provider.LastSymbol);
    }

    [Fact]
    public void Get_UnknownPrice_ReturnsNotFound()
    {
        var controller = new PricesController(new StubLatestPriceProvider(null));

        ActionResult<LatestPriceResponse> result = controller.Get("EURUSD");

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
