namespace PricingAndTrading.Application.Orders.History;

public sealed class OrderHistoryPage
{
    public OrderHistoryPage(
        IEnumerable<OrderHistoryItem> items,
        int page,
        int pageSize,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = Array.AsReadOnly(items.ToArray());
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<OrderHistoryItem> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }
}
