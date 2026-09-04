using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.TradingRules;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class TradingRulesUpdaterTests
{
    [Fact]
    public async Task UpdateAsync_SuccessfulPersistence_UpdatesRuntimeSnapshot()
    {
        TradingRulesConfiguration initial = CreateRules(1_000m);
        TradingRulesConfiguration replacement = CreateRules(2_000m);
        var repository = new RecordingRepository();
        var store = new RecordingStore(initial);
        var updater = new TradingRulesUpdater(repository, store);

        await updater.UpdateAsync(replacement);

        Assert.Same(replacement, repository.SavedRules);
        Assert.Same(replacement, store.Current);
    }

    [Fact]
    public async Task UpdateAsync_SuccessfulPersistence_PersistsBeforeRuntimeReplacement()
    {
        var events = new List<string>();
        var repository = new RecordingRepository(events);
        var store = new RecordingStore(CreateRules(1_000m), events);
        var updater = new TradingRulesUpdater(repository, store);

        await updater.UpdateAsync(CreateRules(2_000m));

        Assert.Equal(["persist", "replace"], events);
    }

    [Fact]
    public async Task UpdateAsync_PersistenceFailure_LeavesRuntimeSnapshotUnchanged()
    {
        TradingRulesConfiguration initial = CreateRules(1_000m);
        var repository = new RecordingRepository
        {
            ExceptionToThrow = new InvalidOperationException("Database unavailable.")
        };
        var store = new RecordingStore(initial);
        var updater = new TradingRulesUpdater(repository, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            updater.UpdateAsync(CreateRules(2_000m)));

        Assert.Same(initial, store.Current);
        Assert.Equal(0, store.UpdateCount);
    }

    [Fact]
    public async Task UpdateAsync_CanceledToken_PropagatesCancellation()
    {
        var repository = new RecordingRepository();
        var store = new RecordingStore(CreateRules(1_000m));
        var updater = new TradingRulesUpdater(repository, store);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            updater.UpdateAsync(CreateRules(2_000m), cancellation.Token));

        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(0, store.UpdateCount);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentCalls_SerializesPersistenceWithinInstance()
    {
        var repository = new BlockingRepository();
        var store = new RecordingStore(CreateRules(1_000m));
        var updater = new TradingRulesUpdater(repository, store);

        Task firstUpdate = updater.UpdateAsync(CreateRules(2_000m));
        await repository.FirstSaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task secondUpdate = updater.UpdateAsync(CreateRules(3_000m));

        await Task.Delay(50);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(1, repository.MaximumConcurrentSaves);

        repository.ReleaseFirstSave.TrySetResult();
        await Task.WhenAll(firstUpdate, secondUpdate);

        Assert.Equal(2, repository.SaveCount);
        Assert.Equal(1, repository.MaximumConcurrentSaves);
    }

    private static TradingRulesConfiguration CreateRules(
        decimal maximumNotionalAmount) =>
        new(
            maximumNotionalAmount,
            maximumQuantity: 100m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: false,
            symbolWhitelist: null,
            autoTradingSpreadThresholdPercent: 0.1m,
            maximumPriceDeviationPercent: 0.8m);

    private sealed class RecordingRepository(List<string>? events = null) :
        ITradingRulesRepository
    {
        public Exception? ExceptionToThrow { get; init; }

        public TradingRulesConfiguration? SavedRules { get; private set; }

        public int SaveCount { get; private set; }

        public Task<TradingRulesConfiguration?> GetAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            TradingRulesConfiguration tradingRules,
            CancellationToken cancellationToken)
        {
            SaveCount++;
            SavedRules = tradingRules;
            events?.Add("persist");

            return ExceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(ExceptionToThrow);
        }
    }

    private sealed class RecordingStore(
        TradingRulesConfiguration initial,
        List<string>? events = null) : ITradingRulesStore
    {
        public TradingRulesConfiguration Current { get; private set; } = initial;

        public int UpdateCount { get; private set; }

        public void Update(TradingRulesConfiguration tradingRules)
        {
            events?.Add("replace");
            Current = tradingRules;
            UpdateCount++;
        }
    }

    private sealed class BlockingRepository : ITradingRulesRepository
    {
        private int _activeSaves;
        private int _saveCount;
        private int _maximumConcurrentSaves;

        public TaskCompletionSource FirstSaveEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstSave { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SaveCount => Volatile.Read(ref _saveCount);

        public int MaximumConcurrentSaves =>
            Volatile.Read(ref _maximumConcurrentSaves);

        public Task<TradingRulesConfiguration?> GetAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public async Task SaveAsync(
            TradingRulesConfiguration tradingRules,
            CancellationToken cancellationToken)
        {
            int saveNumber = Interlocked.Increment(ref _saveCount);
            int activeSaves = Interlocked.Increment(ref _activeSaves);
            InterlockedExtensions.Max(ref _maximumConcurrentSaves, activeSaves);

            try
            {
                if (saveNumber == 1)
                {
                    FirstSaveEntered.TrySetResult();
                    await ReleaseFirstSave.Task.WaitAsync(cancellationToken);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeSaves);
            }
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int target, int value)
        {
            int observed = Volatile.Read(ref target);
            while (observed < value)
            {
                int prior = Interlocked.CompareExchange(ref target, value, observed);
                if (prior == observed)
                {
                    return;
                }

                observed = prior;
            }
        }
    }
}
