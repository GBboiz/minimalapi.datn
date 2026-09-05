using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class GoogleSheetConnectionConfiguration : IEntityTypeConfiguration<GoogleSheetConnection>
{
    public void Configure(EntityTypeBuilder<GoogleSheetConnection> builder)
    {
        builder.ToTable("google_sheet_connections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new GoogleSheetConnectionId(v));

        builder.Property(c => c.StoreId)
            .HasColumnName("store_id")
            .HasConversion(id => id.Value, v => new StoreId(v))
            .IsRequired();

        builder.Property(c => c.SpreadsheetId)
            .HasColumnName("spreadsheet_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.SheetName)
            .HasColumnName("sheet_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.EncryptedRefreshToken)
            .HasColumnName("encrypted_refresh_token")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.LastSyncAt)
            .HasColumnName("last_sync_at");

        builder.Property(c => c.LastErrorMessage)
            .HasColumnName("last_error_message")
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(c => c.StoreId).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
