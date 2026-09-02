using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.UnitTests.Orders;

public sealed class TradeDecisionTests
{
    private static readonly Guid OrderId =
        Guid.Parse("b2c78f51-d3ee-460e-897f-50605e6e918d");

    [Fact]
    public void Accepted_OrderId_CreatesDecisionWithoutRejectionReasons()
    {
        TradeDecision decision = TradeDecision.Accepted(OrderId);

        Assert.Equal(OrderId, decision.OrderId);
        Assert.Equal(OrderStatus.Accepted, decision.Status);
        Assert.Empty(decision.RejectionReasons);
    }

    [Fact]
    public void Accepted_EmptyOrderId_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => TradeDecision.Accepted(Guid.Empty));

        Assert.Equal("orderId", exception.ParamName);
    }

    [Fact]
    public void Rejected_NoRejectionReasons_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TradeDecision.Rejected(OrderId));
    }

    [Fact]
    public void Rejected_StructuredRejectionReason_PreservesReason()
    {
        var reason = new RejectionReason(
            "MAXIMUM_QUANTITY_EXCEEDED",
            "Order quantity exceeds the configured maximum.");

        TradeDecision decision = TradeDecision.Rejected(OrderId, reason);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        RejectionReason actualReason = Assert.Single(decision.RejectionReasons);
        Assert.Equal(reason, actualReason);
    }

    [Fact]
    public void Rejected_EmptyOrderId_ThrowsArgumentException()
    {
        var reason = new RejectionReason("TEST_REJECTION", "Test rejection.");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => TradeDecision.Rejected(Guid.Empty, reason));

        Assert.Equal("orderId", exception.ParamName);
    }
}
