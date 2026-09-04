using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Configurations;

internal sealed class LatestPriceConfiguration :
    IEntityTypeConfiguration<LatestPriceEntity>
{
    public void Configure(EntityTypeBuilder<LatestPriceEntity> builder)
    {
        builder.ToTable("LatestPrices");
        builder.HasKey(price => price.Symbol);
        builder.Property(price => price.Symbol)
            .HasMaxLength(32);
        builder.Property(price => price.BidPrice)
            .HasPrecision(38, 18);
        builder.Property(price => price.AskPrice)
            .HasPrecision(38, 18);
        builder.Property(price => price.CurrentMarketPrice)
            .HasPrecision(38, 18);
        builder.Property(price => price.Spread)
            .HasPrecision(38, 18);
        builder.Property(price => price.SpreadPercent)
            .HasPrecision(38, 18);
        builder.Property(price => price.Timestamp)
            .HasColumnType("timestamp with time zone");
    }
}
