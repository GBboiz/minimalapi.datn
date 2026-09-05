using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class StoreMemberConfiguration : IEntityTypeConfiguration<StoreMember>
{
    public void Configure(EntityTypeBuilder<StoreMember> builder)
    {
        builder.ToTable("store_members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StoreId).HasColumnName("store_id").HasConversion(x => x.Value, x => new StoreId(x));
        builder.Property(x => x.UserId).HasColumnName("user_id").HasConversion(x => x.Value, x => new UserId(x));
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.StoreId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
