using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Requests;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Application.TradingRules;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Api.Controllers;

[ApiController]
[Route("api/trading-rules")]
public sealed class TradingRulesController : ControllerBase
{
    private readonly ITradingRulesProvider _provider;
    private readonly ITradingRulesUpdater _updater;

    public TradingRulesController(
        ITradingRulesProvider provider,
        ITradingRulesUpdater updater)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(updater);

        _provider = provider;
        _updater = updater;
    }

    [HttpGet]
    [ProducesResponseType<GetTradingRulesResponse>(StatusCodes.Status200OK)]
    public ActionResult<GetTradingRulesResponse> Get() =>
        Ok(ApiResponseMapper.ToResponse(_provider.Current));

    [HttpPut]
    [ProducesResponseType<GetTradingRulesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetTradingRulesResponse>> UpdateAsync(
        [FromBody] UpdateTradingRulesRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiValidationProblem.Create(
                "request",
                "A request body is required.");
        }

        TradingRulesConfiguration tradingRules;
        try
        {
            tradingRules = new TradingRulesConfiguration(
                request.MaximumNotionalAmount,
                request.MaximumQuantity,
                request.DuplicateOrderIdCheckEnabled,
                request.SymbolWhitelistEnabled,
                request.SymbolWhitelist,
                request.AutoTradingSpreadThresholdPercent,
                request.MaximumPriceDeviationPercent);
        }
        catch (ArgumentException exception)
        {
            return ApiValidationProblem.Create(
                exception.ParamName ?? "request",
                exception.Message);
        }

        await _updater.UpdateAsync(tradingRules, cancellationToken);
        return Ok(ApiResponseMapper.ToResponse(tradingRules));
    }
}
