using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PricingAndTrading.Infrastructure.Persistence;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class TradingDbContextModelTests
{
    [Fact]
    public void Model_OrderPersistenceIdIsPrimaryKeyAndBusinessOrderIdIsNotUnique()
    {
        using TradingDbContext dbContext = CreateDbContext();
        IEntityType order = GetEntityType<OrderEntity>(dbContext);

        Assert.Equal(
            [nameof(OrderEntity.PersistenceId)],
            order.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(
            order.GetIndexes(),
            index => !index.IsUnique
                && HasProperties(index, nameof(OrderEntity.OrderId)));
        Assert.DoesNotContain(
            order.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Any(
                    property => property.Name == nameof(OrderEntity.OrderId)));
    }

    [Fact]
    public void Model_OrderHistoryIndexes_AreConfigured()
    {
        using TradingDbContext dbContext = CreateDbContext();
        IEntityType order = GetEntityType<OrderEntity>(dbContext);

        Assert.Contains(
            order.GetIndexes(),
            index => HasProperties(
                index,
                nameof(OrderEntity.Symbol),
                nameof(OrderEntity.CreatedAt)));
        Assert.Contains(
            order.GetIndexes(),
            index => HasProperties(
                index,
                nameof(OrderEntity.Status),
                nameof(OrderEntity.CreatedAt)));
    }

    [Fact]
    public void Model_OrderIdRegistrationAndLatestPrice_UseRequiredPrimaryKeys()
    {
        using TradingDbContext dbContext = CreateDbContext();
        IEntityType registration = GetEntityType<OrderIdRegistrationEntity>(dbContext);
        IEntityType latestPrice = GetEntityType<LatestPriceEntity>(dbContext);

        Assert.Equal(
            [nameof(OrderIdRegistrationEntity.OrderId)],
            registration.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(LatestPriceEntity.Symbol)],
            latestPrice.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Model_RejectionReasonSequenceIsPartOfKeyAndCascadeDeleteIsConfigured()
    {
        using TradingDbContext dbContext = CreateDbContext();
        IEntityType rejectionReason = GetEntityType<OrderRejectionReasonEntity>(dbContext);

        Assert.Equal(
            [
                nameof(OrderRejectionReasonEntity.OrderPersistenceId),
                nameof(OrderRejectionReasonEntity.Sequence)
            ],
            rejectionReason.FindPrimaryKey()!.Properties.Select(
                property => property.Name));
        Assert.Equal(
            DeleteBehavior.Cascade,
            Assert.Single(rejectionReason.GetForeignKeys()).DeleteBehavior);
    }

    [Fact]
    public void Model_FinancialDecimals_UseExplicitPrecisionAndScale()
    {
        using TradingDbContext dbContext = CreateDbContext();

        AssertPrecision<OrderEntity>(
            dbContext,
            nameof(OrderEntity.Price),
            nameof(OrderEntity.Quantity));
        AssertPrecision<TradingRulesEntity>(
            dbContext,
            nameof(TradingRulesEntity.MaximumNotionalAmount),
            nameof(TradingRulesEntity.MaximumQuantity),
            nameof(TradingRulesEntity.MaximumPriceDeviationPercent),
            nameof(TradingRulesEntity.AutoTradingSpreadThresholdPercent));
        AssertPrecision<LatestPriceEntity>(
            dbContext,
            nameof(LatestPriceEntity.BidPrice),
            nameof(LatestPriceEntity.AskPrice),
            nameof(LatestPriceEntity.CurrentMarketPrice),
            nameof(LatestPriceEntity.Spread),
            nameof(LatestPriceEntity.SpreadPercent));
    }

    [Fact]
    public void Model_OrderEnums_UseStringConversions()
    {
        using TradingDbContext dbContext = CreateDbContext();
        IEntityType order = GetEntityType<OrderEntity>(dbContext);

        Assert.Equal(
            typeof(string),
            order.FindProperty(nameof(OrderEntity.Side))!
                .GetTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(
            typeof(string),
            order.FindProperty(nameof(OrderEntity.Type))!
                .GetTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(
            typeof(string),
            order.FindProperty(nameof(OrderEntity.Source))!
                .GetTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(
            typeof(string),
            order.FindProperty(nameof(OrderEntity.Status))!
                .GetTypeMapping().Converter!.ProviderClrType);
    }

    private static void AssertPrecision<TEntity>(
        TradingDbContext dbContext,
        params string[] propertyNames)
    {
        IEntityType entityType = GetEntityType<TEntity>(dbContext);

        foreach (string propertyName in propertyNames)
        {
            IProperty property = entityType.FindProperty(propertyName)!;
            Assert.Equal(38, property.GetPrecision());
            Assert.Equal(18, property.GetScale());
        }
    }

    private static bool HasProperties(IIndex index, params string[] propertyNames) =>
        index.Properties.Select(property => property.Name).SequenceEqual(propertyNames);

    private static IEntityType GetEntityType<TEntity>(TradingDbContext dbContext) =>
        dbContext.Model.FindEntityType(typeof(TEntity))!;

    private static TradingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_tests;Username=test;Password=test")
            .Options;

        return new TradingDbContext(options);
    }
}
