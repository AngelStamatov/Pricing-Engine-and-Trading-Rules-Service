namespace PricingAndTrading.Domain.Orders;

public sealed record RejectionReason
{
    public RejectionReason(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code.Trim();
        Message = message.Trim();
    }

    public string Code { get; }

    public string Message { get; }
}
