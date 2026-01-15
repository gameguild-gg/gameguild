using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Assets.Configuration;

/// <summary>
/// EF Core configuration for AssetContent entity.
/// </summary>
public class AssetContentConfiguration : IEntityTypeConfiguration<AssetContent>
{
    public void Configure(EntityTypeBuilder<AssetContent> builder)
    {
        builder.ToTable("AssetContents", "assets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContentHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.BucketName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ObjectKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SizeBytes)
            .IsRequired();

        builder.Property(e => e.Width);
        builder.Property(e => e.Height);

        builder.Property(e => e.VirusScanStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.VirusScanCompletedAt);

        builder.Property(e => e.ModerationStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ReferenceCount)
            .HasDefaultValue(1);

        builder.Property(e => e.MarkedForDeletionAt);

        builder.Property(e => e.IsDeletable)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(e => e.ContentHash)
            .IsUnique()
            .HasDatabaseName("IX_AssetContents_ContentHash");

        builder.HasIndex(e => e.VirusScanStatus)
            .HasDatabaseName("IX_AssetContents_VirusScanStatus");

        builder.HasIndex(e => e.ModerationStatus)
            .HasDatabaseName("IX_AssetContents_ModerationStatus");

        builder.HasIndex(e => new { e.ReferenceCount, e.MarkedForDeletionAt })
            .HasDatabaseName("IX_AssetContents_GC");
    }
}
