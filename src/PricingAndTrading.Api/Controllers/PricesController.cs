using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Api.Controllers;

[ApiController]
[Route("api/prices")]
public sealed class PricesController : ControllerBase
{
    private readonly ILatestPriceProvider _latestPriceProvider;

    public PricesController(ILatestPriceProvider latestPriceProvider)
    {
        ArgumentNullException.ThrowIfNull(latestPriceProvider);
        _latestPriceProvider = latestPriceProvider;
    }

    [HttpGet("{symbol}")]
    [ProducesResponseType<LatestPriceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<LatestPriceResponse> Get(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return ApiValidationProblem.Create(
                nameof(symbol),
                "Symbol must not be empty or whitespace.");
        }

        MarketPrice? marketPrice = _latestPriceProvider.GetLatest(symbol);
        return marketPrice is null
            ? NotFound()
            : Ok(ApiResponseMapper.ToResponse(marketPrice));
    }
}
