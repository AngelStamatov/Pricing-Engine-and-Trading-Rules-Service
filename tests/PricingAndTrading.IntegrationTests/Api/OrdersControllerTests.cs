using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Requests;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Api.Controllers;
using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.IntegrationTests.Api;

public sealed class OrdersControllerTests
{
    [Fact]
    public async Task SubmitAsync_ValidAcceptedRequest_ProcessesOnceAndReturnsOk()
    {
        var processor = new RecordingOrderProcessor();
        var controller = CreateController(processor);
        SubmitOrderRequest request = CreateRequest();
        using var cancellation = new CancellationTokenSource();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            request,
            cancellation.Token);
        DateTimeOffset after = DateTimeOffset.UtcNow;

        SubmitOrderResponse response = GetOkValue(result);
        Assert.Equal(OrderStatus.Accepted, response.Status);
        Assert.Empty(response.RejectionReasons);
        Assert.Equal(1, processor.CallCount);
        Assert.Equal(cancellation.Token, processor.LastCancellationToken);
        Assert.NotNull(processor.LastOrder);
        Assert.Equal(OrderSource.Api, processor.LastOrder.Source);
        Assert.InRange(processor.LastOrder.CreatedAt, before, after);
    }

    [Fact]
    public async Task SubmitAsync_BusinessRejected_ReturnsOkWithStructuredReasons()
    {
        var processor = new RecordingOrderProcessor
        {
            DecisionFactory = order => TradeDecision.Rejected(
                order.Id,
                new RejectionReason(
                    "MaximumQuantityExceeded",
                    "Quantity exceeds the configured maximum."))
        };
        var controller = CreateController(processor);

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            CreateRequest(),
            CancellationToken.None);

        SubmitOrderResponse response = GetOkValue(result);
        Assert.Equal(OrderStatus.Rejected, response.Status);
        RejectionReasonResponse reason = Assert.Single(response.RejectionReasons);
        Assert.Equal("MaximumQuantityExceeded", reason.Code);
        Assert.Equal("Quantity exceeds the configured maximum.", reason.Message);
    }

    [Fact]
    public async Task SubmitAsync_ProcessorFailure_PropagatesSystemFailure()
    {
        var processor = new RecordingOrderProcessor
        {
            ExceptionToThrow = new InvalidOperationException("Database unavailable.")
        };
        var controller = CreateController(processor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.SubmitAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_EmptyOrderId_ReturnsBadRequestWithoutProcessing()
    {
        var processor = new RecordingOrderProcessor();
        var controller = CreateController(processor);
        SubmitOrderRequest request = CreateRequest(orderId: Guid.Empty);

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, processor.CallCount);
    }

    public static TheoryData<decimal, decimal> InvalidFinancialValues =>
        new()
        {
            { 0m, 1m },
            { -1m, 1m },
            { 1m, 0m },
            { 1m, -1m }
        };

    [Theory]
    [MemberData(nameof(InvalidFinancialValues))]
    public async Task SubmitAsync_NonPositivePriceOrQuantity_ReturnsBadRequest(
        decimal price,
        decimal quantity)
    {
        var processor = new RecordingOrderProcessor();
        var controller = CreateController(processor);
        SubmitOrderRequest request = CreateRequest(
            price: price,
            quantity: quantity);

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, processor.CallCount);
    }

    [Theory]
    [InlineData(99, 0)]
    [InlineData(0, 99)]
    public async Task SubmitAsync_UndefinedEnumValue_ReturnsBadRequest(
        int side,
        int type)
    {
        var processor = new RecordingOrderProcessor();
        var controller = CreateController(processor);
        SubmitOrderRequest request = CreateRequest(
            side: (OrderSide)side,
            type: (OrderType)type);

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, processor.CallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubmitAsync_EmptySymbol_ReturnsBadRequestWithoutProcessing(
        string symbol)
    {
        var processor = new RecordingOrderProcessor();
        var controller = CreateController(processor);

        ActionResult<SubmitOrderResponse> result = await controller.SubmitAsync(
            CreateRequest(symbol: symbol),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, processor.CallCount);
    }

    private static OrdersController CreateController(
        RecordingOrderProcessor processor) =>
        new(processor, new RecordingOrderHistoryRepository());

    private static SubmitOrderRequest CreateRequest(
        Guid? orderId = null,
        string symbol = "EURUSD",
        OrderSide side = OrderSide.Buy,
        OrderType type = OrderType.Limit,
        decimal price = 100m,
        decimal quantity = 5m) =>
        new()
        {
            OrderId = orderId ?? Guid.NewGuid(),
            Symbol = symbol,
            Side = side,
            Type = type,
            Price = price,
            Quantity = quantity
        };

    private static TResponse GetOkValue<TResponse>(ActionResult<TResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<TResponse>(ok.Value);
    }
}
