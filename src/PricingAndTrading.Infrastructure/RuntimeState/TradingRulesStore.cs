using PricingAndTrading.Application.TradingRules;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure.RuntimeState;

public sealed class TradingRulesStore : ITradingRulesStore
{
    private TradingRulesConfiguration _current;

    public TradingRulesStore(TradingRulesConfiguration initialTradingRules)
    {
        ArgumentNullException.ThrowIfNull(initialTradingRules);
        _current = initialTradingRules;
    }

    public TradingRulesConfiguration Current => Volatile.Read(ref _current);

    public void Update(TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(tradingRules);
        Interlocked.Exchange(ref _current, tradingRules);
    }
}
