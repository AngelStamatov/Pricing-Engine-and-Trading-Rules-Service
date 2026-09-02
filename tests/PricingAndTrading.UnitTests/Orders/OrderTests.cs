using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.UnitTests.Orders;

public sealed class OrderTests
{
    private static readonly Guid OrderId =
        Guid.Parse("b2c78f51-d3ee-460e-897f-50605e6e918d");

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 2, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ValidValues_CreatesOrder()
    {
        Order order = CreateOrder();

        Assert.Equal(OrderId, order.Id);
        Assert.Equal("EURUSD", order.Symbol);
        Assert.Equal(OrderSide.Buy, order.Side);
        Assert.Equal(OrderType.Limit, order.Type);
        Assert.Equal(100m, order.Price);
        Assert.Equal(5m, order.Quantity);
        Assert.Equal(OrderSource.Api, order.Source);
        Assert.Equal(CreatedAt, order.CreatedAt);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateOrder(id: Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSymbol_ThrowsArgumentException(string? invalidSymbol)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => CreateOrder(symbol: invalidSymbol!));
    }

    [Fact]
    public void Constructor_UndefinedSide_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateOrder(side: (OrderSide)int.MaxValue));
    }

    [Fact]
    public void Constructor_UndefinedType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateOrder(type: (OrderType)int.MaxValue));
    }

    [Fact]
    public void Constructor_UndefinedSource_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateOrder(source: (OrderSource)int.MaxValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositivePrice_ThrowsArgumentOutOfRangeException(
        int invalidPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateOrder(price: invalidPrice));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveQuantity_ThrowsArgumentOutOfRangeException(
        int invalidQuantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateOrder(quantity: invalidQuantity));
    }

    private static Order CreateOrder(
        Guid? id = null,
        string symbol = "EURUSD",
        OrderSide side = OrderSide.Buy,
        OrderType type = OrderType.Limit,
        decimal price = 100m,
        decimal quantity = 5m,
        OrderSource source = OrderSource.Api)
    {
        return new Order(
            id ?? OrderId,
            symbol,
            side,
            type,
            price,
            quantity,
            source,
            CreatedAt);
    }
}
