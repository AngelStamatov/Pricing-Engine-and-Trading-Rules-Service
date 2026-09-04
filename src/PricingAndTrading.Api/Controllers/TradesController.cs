using Microsoft.AspNetCore.Mvc;
using PricingAndTrading.Api.Contracts.Responses;
using PricingAndTrading.Application.Orders.History;
using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public TradesController(IOrderHistoryRepository orderHistoryRepository)
    {
        ArgumentNullException.ThrowIfNull(orderHistoryRepository);
        _orderHistoryRepository = orderHistoryRepository;
    }

    [HttpGet("history")]
    [ProducesResponseType<OrderHistoryPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderHistoryPageResponse>> GetHistoryAsync(
        [FromQuery] string? symbol = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderSource? source = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
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
                from,
                to,
                page,
                pageSize);
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
}
