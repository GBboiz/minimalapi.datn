using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new PromotionId(value));

        builder.Property(p => p.StoreId)
            .HasColumnName("store_id")
            .HasConversion(id => id.Value, value => new StoreId(value));

        builder.HasIndex(p => p.StoreId);

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => new { p.StoreId, p.Code }).IsUnique();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasColumnName("discount_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.Value)
            .HasColumnName("value")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Ignore(p => p.DomainEvents);
    }
}
