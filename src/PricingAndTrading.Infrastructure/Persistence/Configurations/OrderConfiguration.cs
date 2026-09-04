using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.PersistenceId);

        builder.Property(order => order.Symbol)
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(order => order.Side)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(order => order.Type)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(order => order.Price)
            .HasPrecision(38, 18);
        builder.Property(order => order.Quantity)
            .HasPrecision(38, 18);
        builder.Property(order => order.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(order => order.CreatedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(order => order.OrderId);
        builder.HasIndex(order => new { order.Symbol, order.CreatedAt });
        builder.HasIndex(order => new { order.Status, order.CreatedAt });
    }
}
