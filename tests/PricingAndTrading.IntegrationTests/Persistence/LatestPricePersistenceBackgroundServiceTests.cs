using Microsoft.Extensions.Logging;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class LatestPricePersistenceBackgroundServiceTests
{
    private static readonly MarketPrice Price = MarketPrice.From(
        new PriceTick(
            "EURUSD",
            99m,
            101m,
            new DateTimeOffset(2026, 9, 3, 17, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task PersistSnapshotAsync_TransientPersistenceFailure_NextInvocationTriesAgain()
    {
        var repository = new RecordingPriceStateRepository(failuresRemaining: 1);
        var logger = new RecordingLogger<LatestPricePersistenceBackgroundService>();
        var service = CreateService(repository, logger);

        await service.PersistSnapshotAsync(CancellationToken.None);
        await service.PersistSnapshotAsync(CancellationToken.None);

        Assert.Equal(2, repository.InvocationCount);
        Assert.Equal(1, repository.SuccessCount);
        Assert.Equal([LogLevel.Warning], logger.Levels);
    }

    [Fact]
    public async Task PersistSnapshotAsync_StoppingTokenCancellation_PropagatesOperationCanceledException()
    {
        var repository = new RecordingPriceStateRepository(failuresRemaining: 0);
        var service = CreateService(
            repository,
            new RecordingLogger<LatestPricePersistenceBackgroundService>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.PersistSnapshotAsync(cancellation.Token));
    }

    private static LatestPricePersistenceBackgroundService CreateService(
        IPriceStateRepository repository,
        ILogger<LatestPricePersistenceBackgroundService> logger) =>
        new(
            new FixedSnapshotProvider([Price]),
            repository,
            logger);

    private sealed class FixedSnapshotProvider(
        IReadOnlyCollection<MarketPrice> snapshot) :
        ILatestPriceSnapshotProvider
    {
        public IReadOnlyCollection<MarketPrice> GetSnapshot() => snapshot;
    }

    private sealed class RecordingPriceStateRepository(int failuresRemaining) :
        IPriceStateRepository
    {
        private int _failuresRemaining = failuresRemaining;

        public int InvocationCount { get; private set; }

        public int SuccessCount { get; private set; }

        public Task<IReadOnlyList<MarketPrice>> GetAllAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpsertLatestAsync(
            IReadOnlyCollection<MarketPrice> prices,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            cancellationToken.ThrowIfCancellationRequested();

            if (_failuresRemaining > 0)
            {
                _failuresRemaining--;
                throw new InvalidOperationException("Temporary database failure.");
            }

            SuccessCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
        }
    }
}
