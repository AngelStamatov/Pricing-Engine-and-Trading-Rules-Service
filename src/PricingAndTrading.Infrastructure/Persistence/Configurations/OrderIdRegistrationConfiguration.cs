using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Configurations;

internal sealed class OrderIdRegistrationConfiguration :
    IEntityTypeConfiguration<OrderIdRegistrationEntity>
{
    public void Configure(EntityTypeBuilder<OrderIdRegistrationEntity> builder)
    {
        builder.ToTable("OrderIdRegistrations");
        builder.HasKey(registration => registration.OrderId);
        builder.Property(registration => registration.RegisteredAt)
            .HasColumnType("timestamp with time zone");
    }
}
