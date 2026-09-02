using PricingAndTrading.Domain.Orders;

namespace PricingAndTrading.Application.TradingRules;

public sealed class TradeValidationResult
{
    private TradeValidationResult(IReadOnlyList<RejectionReason> rejectionReasons)
    {
        RejectionReasons = rejectionReasons;
    }

    public bool IsValid => RejectionReasons.Count == 0;

    public IReadOnlyList<RejectionReason> RejectionReasons { get; }

    public static TradeValidationResult Valid()
    {
        return new TradeValidationResult(Array.Empty<RejectionReason>());
    }

    public static TradeValidationResult Invalid(
        params RejectionReason[] rejectionReasons)
    {
        ArgumentNullException.ThrowIfNull(rejectionReasons);

        if (rejectionReasons.Length == 0)
        {
            throw new ArgumentException(
                "An invalid validation result must contain at least one rejection reason.",
                nameof(rejectionReasons));
        }

        if (rejectionReasons.Any(static reason => reason is null))
        {
            throw new ArgumentException(
                "Rejection reasons must not contain null values.",
                nameof(rejectionReasons));
        }

        return new TradeValidationResult(
            Array.AsReadOnly(rejectionReasons.ToArray()));
    }
}
