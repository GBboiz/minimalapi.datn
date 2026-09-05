using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.StoreId)
            .HasColumnName("store_id")
            .HasConversion(id => id.Value, v => new StoreId(v))
            .IsRequired();

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnName("payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.OccurredAt)
            .HasColumnName("occurred_at");

        builder.Property(m => m.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(m => m.Error)
            .HasColumnName("error")
            .HasColumnType("text");

        builder.HasIndex(m => m.StoreId);
        builder.HasIndex(m => new { m.ProcessedAt, m.RetryCount });
    }
}
