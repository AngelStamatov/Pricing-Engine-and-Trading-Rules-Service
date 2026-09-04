using PricingAndTrading.Application.Abstractions;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public sealed class TradingRulesUpdater : ITradingRulesUpdater
{
    private readonly ITradingRulesRepository _repository;
    private readonly ITradingRulesStore _store;
    // This serializes updates in one process; cross-instance coordination is
    // outside the current single-instance MVP.
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    public TradingRulesUpdater(
        ITradingRulesRepository repository,
        ITradingRulesStore store)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(store);

        _repository = repository;
        _store = store;
    }

    public async Task UpdateAsync(
        TradingRulesConfiguration tradingRules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tradingRules);

        await _updateLock.WaitAsync(cancellationToken);
        try
        {
            await _repository.SaveAsync(tradingRules, cancellationToken);
            _store.Update(tradingRules);
        }
        finally
        {
            _updateLock.Release();
        }
    }
}
