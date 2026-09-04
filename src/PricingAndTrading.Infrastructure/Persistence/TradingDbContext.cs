using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence;

public sealed class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    internal DbSet<OrderEntity> Orders => Set<OrderEntity>();

    internal DbSet<OrderRejectionReasonEntity> OrderRejectionReasons =>
        Set<OrderRejectionReasonEntity>();

    internal DbSet<OrderIdRegistrationEntity> OrderIdRegistrations =>
        Set<OrderIdRegistrationEntity>();

    internal DbSet<TradingRulesEntity> TradingRules => Set<TradingRulesEntity>();

    internal DbSet<LatestPriceEntity> LatestPrices => Set<LatestPriceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}
