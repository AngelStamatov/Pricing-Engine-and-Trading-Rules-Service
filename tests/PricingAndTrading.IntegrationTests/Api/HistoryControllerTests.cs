using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Api.Controllers;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.IntegrationTests.Api;

public sealed class HistoryControllerTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 4, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTradeHistoryAsync_ValidFilters_PassesNormalizedQueryToRepository()
    {
        var repository = new RecordingOrderHistoryRepository
        {
            Result = CreatePage()
        };
        var controller = new TradesController(repository);
        DateTimeOffset from = Timestamp.AddMinutes(-1);
        DateTimeOffset to = Timestamp.AddMinutes(1);

        ActionResult<OrderHistoryPageResponse> result =
            await controller.GetHistoryAsync(
                " eurusd ",
                OrderStatus.Rejected,
                OrderSource.Api,
                from,
                to,
                page: 2,
                pageSize: 25);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, repository.CallCount);
        Assert.NotNull(repository.LastQuery);
        Assert.Equal("EURUSD", repository.LastQuery.Symbol);
        Assert.Equal(OrderStatus.Rejected, repository.LastQuery.Status);
        Assert.Equal(OrderSource.Api, repository.LastQuery.Source);
        Assert.Equal(from, repository.LastQuery.From);
        Assert.Equal(to, repository.LastQuery.To);
        Assert.Equal(2, repository.LastQuery.Page);
        Assert.Equal(25, repository.LastQuery.PageSize);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetTradeHistoryAsync_InvalidPagination_ReturnsBadRequest(
        int page,
        int pageSize)
    {
        var repository = new RecordingOrderHistoryRepository();
        var controller = new TradesController(repository);

        ActionResult<OrderHistoryPageResponse> result =
            await controller.GetHistoryAsync(page: page, pageSize: pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetTradeHistoryAsync_FromAfterTo_ReturnsBadRequest()
    {
        var repository = new RecordingOrderHistoryRepository();
        var controller = new TradesController(repository);

        ActionResult<OrderHistoryPageResponse> result =
            await controller.GetHistoryAsync(
                from: Timestamp.AddMinutes(1),
                to: Timestamp);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetSymbolHistoryAsync_RouteSymbolForcesQueryFilter()
    {
        var repository = new RecordingOrderHistoryRepository
        {
            Result = CreatePage()
        };
        var controller = new OrdersController(
            new RecordingOrderProcessor(),
            repository);

        ActionResult<OrderHistoryPageResponse> result =
            await controller.GetHistoryAsync(
                " gbpusd ",
                page: 1,
                pageSize: 10);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal("GBPUSD", repository.LastQuery!.Symbol);
        Assert.Equal(1, repository.LastQuery.Page);
        Assert.Equal(10, repository.LastQuery.PageSize);
    }

    private static OrderHistoryPage CreatePage()
    {
        var item = new OrderHistoryItem(
            Guid.NewGuid(),
            "EURUSD",
            OrderSide.Buy,
            OrderType.Limit,
            100m,
            5m,
            OrderSource.Api,
            Timestamp,
            OrderStatus.Accepted,
            []);

        return new OrderHistoryPage([item], page: 1, pageSize: 50, totalCount: 1);
    }
}
