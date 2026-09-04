using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Application.Orders.History;

public sealed class OrderHistoryQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;

    public OrderHistoryQuery(
        string? symbol = null,
        OrderStatus? status = null,
        OrderSource? source = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        if (symbol is not null && string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException(
                "Symbol must not be empty or whitespace.",
                nameof(symbol));
        }

        if (status is not null && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        if (source is not null && !Enum.IsDefined(source.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                page,
                "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                pageSize,
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        if (from is not null && to is not null && from > to)
        {
            throw new ArgumentException(
                "From must be earlier than or equal to To.",
                nameof(from));
        }

        Symbol = symbol?.Trim().ToUpperInvariant();
        Status = status;
        Source = source;
        From = from;
        To = to;
        Page = page;
        PageSize = pageSize;
    }

    public string? Symbol { get; }

    public OrderStatus? Status { get; }

    public OrderSource? Source { get; }

    public DateTimeOffset? From { get; }

    public DateTimeOffset? To { get; }

    public int Page { get; }

    public int PageSize { get; }
}
