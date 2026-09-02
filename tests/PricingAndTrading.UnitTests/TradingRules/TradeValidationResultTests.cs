using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class TradeValidationResultTests
{
    [Fact]
    public void Valid_NoRejectionReasons_ReturnsValidEmptyResult()
    {
        TradeValidationResult result = TradeValidationResult.Valid();

        Assert.True(result.IsValid);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void Invalid_SuppliedRejectionReasons_ReturnsInvalidResultWithReasons()
    {
        RejectionReason firstReason = CreateReason("FirstFailure");
        RejectionReason secondReason = CreateReason("SecondFailure");

        TradeValidationResult result =
            TradeValidationResult.Invalid(firstReason, secondReason);

        Assert.False(result.IsValid);
        Assert.Equal([firstReason, secondReason], result.RejectionReasons);
    }

    [Fact]
    public void Invalid_NoRejectionReasons_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => TradeValidationResult.Invalid());
    }

    [Fact]
    public void Invalid_NullRejectionReasons_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => TradeValidationResult.Invalid(null!));
    }

    [Fact]
    public void Invalid_NullRejectionReasonEntry_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => TradeValidationResult.Invalid([null!]));
    }

    [Fact]
    public void Invalid_SourceArrayMutated_PreservesOriginalSnapshot()
    {
        RejectionReason originalReason = CreateReason("OriginalFailure");
        RejectionReason[] sourceReasons = [originalReason];
        TradeValidationResult result =
            TradeValidationResult.Invalid(sourceReasons);

        sourceReasons[0] = CreateReason("ReplacementFailure");

        Assert.Same(originalReason, Assert.Single(result.RejectionReasons));
    }

    private static RejectionReason CreateReason(string code)
    {
        return new RejectionReason(code, $"{code} occurred.");
    }
}
