using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Requests;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Api.Controllers;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.Api;

public sealed class TradingRulesControllerTests
{
    [Fact]
    public void Get_CurrentRuntimeSnapshot_ReturnsOkResponse()
    {
        TradingRulesConfiguration current = CreateRules(1_000m);
        var controller = new TradingRulesController(
            new StubTradingRulesProvider(current),
            new RecordingTradingRulesUpdater());

        ActionResult<GetTradingRulesResponse> result = controller.Get();

        GetTradingRulesResponse response = GetOkValue(result);
        Assert.Equal(current.MaximumNotionalAmount, response.MaximumNotionalAmount);
        Assert.Equal(current.SymbolWhitelist, response.SymbolWhitelist);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_InvokesUpdaterAndReturnsNormalizedSnapshot()
    {
        var updater = new RecordingTradingRulesUpdater();
        var controller = new TradingRulesController(
            new StubTradingRulesProvider(CreateRules(1_000m)),
            updater);
        UpdateTradingRulesRequest request = CreateRequest();
        using var cancellation = new CancellationTokenSource();

        ActionResult<GetTradingRulesResponse> result = await controller.UpdateAsync(
            request,
            cancellation.Token);

        GetTradingRulesResponse response = GetOkValue(result);
        Assert.Equal(1, updater.CallCount);
        Assert.Equal(cancellation.Token, updater.LastCancellationToken);
        Assert.NotNull(updater.LastRules);
        Assert.Equal(["EURUSD", "GBPUSD"], updater.LastRules.SymbolWhitelist);
        Assert.Equal(updater.LastRules.MaximumNotionalAmount, response.MaximumNotionalAmount);
    }

    [Fact]
    public async Task UpdateAsync_InvalidDomainConfiguration_ReturnsBadRequestWithoutUpdating()
    {
        var updater = new RecordingTradingRulesUpdater();
        var controller = new TradingRulesController(
            new StubTradingRulesProvider(CreateRules(1_000m)),
            updater);
        UpdateTradingRulesRequest request = CreateRequest(maximumQuantity: 0m);

        ActionResult<GetTradingRulesResponse> result = await controller.UpdateAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, updater.CallCount);
    }

    [Fact]
    public async Task UpdateAsync_UpdaterFailure_PropagatesSystemFailure()
    {
        var updater = new RecordingTradingRulesUpdater
        {
            ExceptionToThrow = new InvalidOperationException("Database unavailable.")
        };
        var controller = new TradingRulesController(
            new StubTradingRulesProvider(CreateRules(1_000m)),
            updater);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.UpdateAsync(CreateRequest(), CancellationToken.None));
    }

    private static TradingRulesConfiguration CreateRules(
        decimal maximumNotionalAmount) =>
        new(
            maximumNotionalAmount,
            maximumQuantity: 100m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: true,
            symbolWhitelist: ["EURUSD", "GBPUSD"],
            autoTradingSpreadThresholdPercent: 0.1m,
            maximumPriceDeviationPercent: 0.8m);

    private static UpdateTradingRulesRequest CreateRequest(
        decimal maximumQuantity = 100m) =>
        new()
        {
            MaximumNotionalAmount = 2_000m,
            MaximumQuantity = maximumQuantity,
            MaximumPriceDeviationPercent = 0.9m,
            DuplicateOrderIdCheckEnabled = true,
            SymbolWhitelistEnabled = true,
            SymbolWhitelist = [" gbpusd ", "eurusd"],
            AutoTradingSpreadThresholdPercent = 0.2m
        };

    private static TResponse GetOkValue<TResponse>(ActionResult<TResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<TResponse>(ok.Value);
    }
}
