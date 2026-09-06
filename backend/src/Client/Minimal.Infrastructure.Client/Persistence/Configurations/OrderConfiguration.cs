using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new OrderId(v));

        builder.Property(o => o.StoreId)
            .HasColumnName("store_id")
            .HasConversion(id => id.Value, v => new StoreId(v));

        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, v => new CustomerId(v));

        builder.Property(o => o.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.ComplexProperty(o => o.SubTotal, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("sub_total_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("sub_total_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(o => o.DiscountPercent)
            .HasColumnName("discount_percent")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);

        builder.ComplexProperty(o => o.DiscountAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("discount_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("discount_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.ComplexProperty(o => o.TotalAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(o => o.StoreId);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.StoreId, o.Code }).IsUnique();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
    }
}
