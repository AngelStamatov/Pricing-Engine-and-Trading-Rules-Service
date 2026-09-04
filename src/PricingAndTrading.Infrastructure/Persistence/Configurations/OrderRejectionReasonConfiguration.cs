using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Configurations;

internal sealed class OrderRejectionReasonConfiguration :
    IEntityTypeConfiguration<OrderRejectionReasonEntity>
{
    public void Configure(EntityTypeBuilder<OrderRejectionReasonEntity> builder)
    {
        builder.ToTable("OrderRejectionReasons");
        builder.HasKey(reason => new { reason.OrderPersistenceId, reason.Sequence });

        builder.Property(reason => reason.Code)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(reason => reason.Message)
            .HasMaxLength(2_000)
            .IsRequired();

        builder.HasOne(reason => reason.Order)
            .WithMany(order => order.RejectionReasons)
            .HasForeignKey(reason => reason.OrderPersistenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
