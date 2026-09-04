using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Entities;
using PricingAndTrading.Infrastructure.Persistence.Repositories;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.Persistence.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 17, 0, 0, TimeSpan.Zero);

    [PostgreSqlFact]
    public async Task Migrations_CleanDatabase_AppliesAndCoreTablesAreQueryable()
    {
        await fixture.RecreateAndMigrateAsync();

        await using TradingDbContext dbContext =
            await fixture.CreateDbContextFactory().CreateDbContextAsync();

        string[] appliedMigrations =
            (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Equal(0, await dbContext.Orders.CountAsync());
        Assert.Equal(0, await dbContext.OrderRejectionReasons.CountAsync());
        Assert.Equal(0, await dbContext.OrderIdRegistrations.CountAsync());
        Assert.Equal(0, await dbContext.TradingRules.CountAsync());
        Assert.Equal(0, await dbContext.LatestPrices.CountAsync());
    }

    [PostgreSqlFact]
    public async Task OrderRepository_AcceptedRejectedAndRepeatedBusinessId_PersistsAllSubmissionsAndReasons()
    {
        await fixture.ResetDataAsync();
        IDbContextFactory<TradingDbContext> factory =
            fixture.CreateDbContextFactory();
        var repository = new PostgresOrderRepository(factory);
        Guid repeatedOrderId = Guid.NewGuid();
        Guid rejectedOrderId = Guid.NewGuid();

        Order firstAccepted = CreateOrder(repeatedOrderId, Timestamp);
        Order secondAccepted = CreateOrder(
            repeatedOrderId,
            Timestamp.AddSeconds(1));
        Order rejected = CreateOrder(rejectedOrderId, Timestamp.AddSeconds(2));
        TradeDecision rejectedDecision = TradeDecision.Rejected(
            rejectedOrderId,
            new RejectionReason("MAX_QUANTITY", "Quantity exceeds the configured maximum."),
            new RejectionReason("PRICE_DEVIATION", "Price deviation exceeds the configured maximum."));

        await repository.SaveAsync(
            firstAccepted,
            TradeDecision.Accepted(repeatedOrderId),
            CancellationToken.None);
        await repository.SaveAsync(
            secondAccepted,
            TradeDecision.Accepted(repeatedOrderId),
            CancellationToken.None);
        await repository.SaveAsync(
            rejected,
            rejectedDecision,
            CancellationToken.None);

        await using TradingDbContext dbContext =
            await factory.CreateDbContextAsync();
        List<OrderEntity> persistedOrders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.RejectionReasons)
            .ToListAsync();

        Assert.Equal(3, persistedOrders.Count);
        OrderEntity[] repeatedOrders = persistedOrders
            .Where(order => order.OrderId == repeatedOrderId)
            .ToArray();
        Assert.Equal(2, repeatedOrders.Length);
        Assert.Equal(2, repeatedOrders.Select(order => order.PersistenceId).Distinct().Count());
        Assert.All(repeatedOrders, order => Assert.NotEqual(Guid.Empty, order.PersistenceId));
        Assert.All(repeatedOrders, order => Assert.Equal(OrderStatus.Accepted, order.Status));

        OrderEntity persistedRejected = Assert.Single(
            persistedOrders,
            order => order.OrderId == rejectedOrderId);
        Assert.NotEqual(rejectedOrderId, persistedRejected.PersistenceId);
        Assert.Equal(OrderStatus.Rejected, persistedRejected.Status);

        OrderRejectionReasonEntity[] reasons = persistedRejected.RejectionReasons
            .OrderBy(reason => reason.Sequence)
            .ToArray();
        Assert.Collection(
            reasons,
            reason =>
            {
                Assert.Equal(0, reason.Sequence);
                Assert.Equal("MAX_QUANTITY", reason.Code);
                Assert.Equal("Quantity exceeds the configured maximum.", reason.Message);
            },
            reason =>
            {
                Assert.Equal(1, reason.Sequence);
                Assert.Equal("PRICE_DEVIATION", reason.Code);
                Assert.Equal("Price deviation exceeds the configured maximum.", reason.Message);
            });
    }

    [PostgreSqlFact]
    public async Task OrderIdRegistry_RepeatedRegistration_ReturnsTrueThenFalse()
    {
        await fixture.ResetDataAsync();
        var registry = new PostgresOrderIdRegistry(
            fixture.CreateDbContextFactory());
        Guid orderId = Guid.NewGuid();

        bool firstResult = await registry.TryRegisterAsync(
            orderId,
            CancellationToken.None);
        bool secondResult = await registry.TryRegisterAsync(
            orderId,
            CancellationToken.None);

        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    [PostgreSqlFact]
    public async Task OrderIdRegistry_ConcurrentRegistration_HasExactlyOneWinnerWithoutErrors()
    {
        const int concurrency = 20;
        await fixture.ResetDataAsync();
        var registry = new PostgresOrderIdRegistry(
            fixture.CreateDbContextFactory());
        Guid orderId = Guid.NewGuid();

        Task<bool>[] registrations = Enumerable.Range(0, concurrency)
            .Select(_ => registry.TryRegisterAsync(
                orderId,
                CancellationToken.None).AsTask())
            .ToArray();

        bool[] results = await Task.WhenAll(registrations);

        Assert.Single(results, result => result);
        Assert.Equal(concurrency - 1, results.Count(result => !result));
    }

    [PostgreSqlFact]
    public async Task TradingRulesRepository_SaveThenLoad_RoundTripsEveryFieldAndTextArray()
    {
        await fixture.ResetDataAsync();
        var repository = new PostgresTradingRulesRepository(
            fixture.CreateDbContextFactory());
        var expected = new TradingRulesConfiguration(
            maximumNotionalAmount: 250_000.125m,
            maximumQuantity: 12_500.5m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: true,
            symbolWhitelist: [" gbpusd ", "EURUSD"],
            autoTradingSpreadThresholdPercent: 0.025m,
            maximumPriceDeviationPercent: 0.875m);

        await repository.SaveAsync(expected, CancellationToken.None);
        TradingRulesConfiguration? actual = await repository.GetAsync(
            CancellationToken.None);

        Assert.NotNull(actual);
        Assert.Equal(expected.MaximumNotionalAmount, actual.MaximumNotionalAmount);
        Assert.Equal(expected.MaximumQuantity, actual.MaximumQuantity);
        Assert.Equal(
            expected.MaximumPriceDeviationPercent,
            actual.MaximumPriceDeviationPercent);
        Assert.Equal(
            expected.DuplicateOrderIdCheckEnabled,
            actual.DuplicateOrderIdCheckEnabled);
        Assert.Equal(expected.SymbolWhitelistEnabled, actual.SymbolWhitelistEnabled);
        Assert.Equal(expected.SymbolWhitelist.ToArray(), actual.SymbolWhitelist.ToArray());
        Assert.Equal(
            expected.AutoTradingSpreadThresholdPercent,
            actual.AutoTradingSpreadThresholdPercent);
    }

    [PostgreSqlFact]
    public async Task PriceStateRepository_UpsertSameSymbol_RoundTripsAndUpdatesSingleRow()
    {
        await fixture.ResetDataAsync();
        IDbContextFactory<TradingDbContext> factory =
            fixture.CreateDbContextFactory();
        var repository = new PostgresPriceStateRepository(factory);
        MarketPrice first = CreateMarketPrice(99m, 101m, Timestamp);
        MarketPrice newer = CreateMarketPrice(
            98m,
            102m,
            Timestamp.AddSeconds(1));

        await repository.UpsertLatestAsync([first], CancellationToken.None);
        MarketPrice firstRoundTrip = Assert.Single(
            await repository.GetAllAsync(CancellationToken.None));
        AssertMarketPrice(first, firstRoundTrip);

        await repository.UpsertLatestAsync([newer], CancellationToken.None);
        MarketPrice newerRoundTrip = Assert.Single(
            await repository.GetAllAsync(CancellationToken.None));

        AssertMarketPrice(newer, newerRoundTrip);
        await using TradingDbContext dbContext = await factory.CreateDbContextAsync();
        LatestPriceEntity persistedPrice = await dbContext.LatestPrices
            .AsNoTracking()
            .SingleAsync(price => price.Symbol == newer.Symbol);
        Assert.Equal(newer.BidPrice, persistedPrice.BidPrice);
        Assert.Equal(newer.AskPrice, persistedPrice.AskPrice);
        Assert.Equal(newer.CurrentMarketPrice, persistedPrice.CurrentMarketPrice);
        Assert.Equal(newer.Spread, persistedPrice.Spread);
        Assert.Equal(newer.SpreadPercent, persistedPrice.SpreadPercent);
        Assert.Equal(newer.Timestamp, persistedPrice.Timestamp);
    }

    private static Order CreateOrder(Guid orderId, DateTimeOffset createdAt) =>
        new(
            orderId,
            "EURUSD",
            OrderSide.Buy,
            OrderType.Limit,
            100.25m,
            5.5m,
            OrderSource.Api,
            createdAt);

    private static MarketPrice CreateMarketPrice(
        decimal bidPrice,
        decimal askPrice,
        DateTimeOffset timestamp) =>
        MarketPrice.From(new PriceTick("EURUSD", bidPrice, askPrice, timestamp));

    private static void AssertMarketPrice(
        MarketPrice expected,
        MarketPrice actual)
    {
        Assert.Equal(expected.Symbol, actual.Symbol);
        Assert.Equal(expected.BidPrice, actual.BidPrice);
        Assert.Equal(expected.AskPrice, actual.AskPrice);
        Assert.Equal(expected.CurrentMarketPrice, actual.CurrentMarketPrice);
        Assert.Equal(expected.Spread, actual.Spread);
        Assert.Equal(expected.SpreadPercent, actual.SpreadPercent);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
    }
}
