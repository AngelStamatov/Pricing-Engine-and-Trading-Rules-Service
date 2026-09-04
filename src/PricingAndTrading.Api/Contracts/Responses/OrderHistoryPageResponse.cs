namespace PricingAndTrading.Api.Contracts.Responses;

public sealed record OrderHistoryPageResponse(
    IReadOnlyList<OrderHistoryItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
