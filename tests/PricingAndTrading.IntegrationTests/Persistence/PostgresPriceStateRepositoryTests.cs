using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Repositories;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class PostgresPriceStateRepositoryTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpsertLatestAsync_DuplicateNormalizedSymbols_ThrowsArgumentExceptionBeforeDatabaseAccess()
    {
        var repository = new PostgresPriceStateRepository(
            new UnusedDbContextFactory());
        MarketPrice first = CreateMarketPrice("eurusd", 99m, 101m);
        MarketPrice duplicate = CreateMarketPrice(" EURUSD ", 98m, 102m);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => repository.UpsertLatestAsync(
                [first, duplicate],
                CancellationToken.None));

        Assert.Equal("prices", exception.ParamName);
    }

    private static MarketPrice CreateMarketPrice(
        string symbol,
        decimal bidPrice,
        decimal askPrice) =>
        MarketPrice.From(new PriceTick(symbol, bidPrice, askPrice, Timestamp));

    private sealed class UnusedDbContextFactory : IDbContextFactory<TradingDbContext>
    {
        public TradingDbContext CreateDbContext() =>
            throw new InvalidOperationException("Database access was not expected.");
    }
}
