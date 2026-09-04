using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Configurations;

internal sealed class TradingRulesConfiguration :
    IEntityTypeConfiguration<TradingRulesEntity>
{
    public void Configure(EntityTypeBuilder<TradingRulesEntity> builder)
    {
        builder.ToTable("TradingRules");
        builder.HasKey(rules => rules.Id);
        builder.Property(rules => rules.Id)
            .ValueGeneratedNever();
        builder.Property(rules => rules.MaximumNotionalAmount)
            .HasPrecision(38, 18);
        builder.Property(rules => rules.MaximumQuantity)
            .HasPrecision(38, 18);
        builder.Property(rules => rules.MaximumPriceDeviationPercent)
            .HasPrecision(38, 18);
        builder.Property(rules => rules.SymbolWhitelist)
            .HasColumnType("text[]")
            .IsRequired();
        builder.Property(rules => rules.AutoTradingSpreadThresholdPercent)
            .HasPrecision(38, 18);
    }
}
