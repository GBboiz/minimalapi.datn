using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        // ProductId ↔ Guid conversion
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ProductId(value));

        builder.Property(p => p.StoreId)
            .HasColumnName("store_id")
            .HasConversion(id => id.Value, value => new StoreId(value));

        builder.HasIndex(p => p.StoreId);

        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(64).IsRequired();
        builder.HasIndex(p => new { p.StoreId, p.Sku }).IsUnique();
        builder.Property(p => p.StockQuantity).HasColumnName("stock_quantity").HasDefaultValue(0);
        builder.Property(p => p.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0);

        // ProductName — ComplexProperty (Value Object owned)
        builder.ComplexProperty(p => p.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(ProductName.MaxLength)
                .IsRequired();
        });

        // Money — ComplexProperty (Value Object owned)
        builder.ComplexProperty(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // CategoryId conversion
        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id")
            .HasConversion(id => id.Value, value => new CategoryId(value));

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active");

        builder.Property(p => p.GiftProductId)
            .HasColumnName("gift_product_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ProductId(value.Value) : (ProductId?)null)
            .IsRequired(false);

        builder.Property(p => p.GiftProductName)
            .HasColumnName("gift_product_name")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        // Ignore DomainEvents & ForecastStock — không persist vào DB
        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.ForecastStock);
    }
}
