using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Requests;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderProcessor _orderProcessor;
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public OrdersController(
        IOrderProcessor orderProcessor,
        IOrderHistoryRepository orderHistoryRepository)
    {
        ArgumentNullException.ThrowIfNull(orderProcessor);
        ArgumentNullException.ThrowIfNull(orderHistoryRepository);

        _orderProcessor = orderProcessor;
        _orderHistoryRepository = orderHistoryRepository;
    }

    [HttpPost]
    [ProducesResponseType<SubmitOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitOrderResponse>> SubmitAsync(
        [FromBody] SubmitOrderRequest? request,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string[]> errors = Validate(request);
        if (errors.Count > 0)
        {
            return ApiValidationProblem.Create(errors);
        }

        var order = new Order(
            request!.OrderId,
            request.Symbol,
            request.Side,
            request.Type,
            request.Price,
            request.Quantity,
            OrderSource.Api,
            DateTimeOffset.UtcNow);

        TradeDecision decision = await _orderProcessor.ProcessAsync(
            order,
            cancellationToken);

        return Ok(ApiResponseMapper.ToResponse(decision));
    }

    [HttpGet("{symbol}/history")]
    [ProducesResponseType<OrderHistoryPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderHistoryPageResponse>> GetHistoryAsync(
        string symbol,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderSource? source = null,
        [FromQuery] int page = OrderHistoryQuery.DefaultPage,
        [FromQuery] int pageSize = OrderHistoryQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        OrderHistoryQuery query;
        try
        {
            query = new OrderHistoryQuery(
                symbol,
                status,
                source,
                page: page,
                pageSize: pageSize);
        }
        catch (ArgumentException exception)
        {
            return ApiValidationProblem.Create(
                exception.ParamName ?? "query",
                exception.Message);
        }

        OrderHistoryPage result = await _orderHistoryRepository.GetAsync(
            query,
            cancellationToken);
        return Ok(ApiResponseMapper.ToResponse(result));
    }

    private static IReadOnlyDictionary<string, string[]> Validate(
        SubmitOrderRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request is null)
        {
            errors["request"] = ["A request body is required."];
            return errors;
        }

        if (request.OrderId == Guid.Empty)
        {
            errors[nameof(request.OrderId)] = ["OrderId must not be empty."];
        }

        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            errors[nameof(request.Symbol)] = ["Symbol must not be empty or whitespace."];
        }

        if (!Enum.IsDefined(request.Side))
        {
            errors[nameof(request.Side)] = ["Side must be a defined value."];
        }

        if (!Enum.IsDefined(request.Type))
        {
            errors[nameof(request.Type)] = ["Type must be a defined value."];
        }

        if (request.Price <= 0)
        {
            errors[nameof(request.Price)] = ["Price must be greater than zero."];
        }

        if (request.Quantity <= 0)
        {
            errors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        }

        return errors;
    }
}
