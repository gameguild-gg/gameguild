using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Assets.Configuration;

/// <summary>
/// EF Core configuration for TransformedAsset entity.
/// </summary>
public class TransformedAssetConfiguration : IEntityTypeConfiguration<TransformedAsset>
{
    public void Configure(EntityTypeBuilder<TransformedAsset> builder)
    {
        builder.ToTable("transformed_assets", "assets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SourceContentId)
            .IsRequired();

        builder.Property(e => e.TransformationSpec)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ObjectKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.SizeBytes)
            .IsRequired();

        builder.Property(e => e.Width)
            .IsRequired();

        builder.Property(e => e.Height)
            .IsRequired();

        builder.Property(e => e.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastAccessedAt)
            .IsRequired();

        // Relationships
        builder.HasOne<AssetContent>()
            .WithMany()
            .HasForeignKey(e => e.SourceContentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.SourceContentId, e.TransformationSpec })
            .IsUnique()
            .HasDatabaseName("IX_TransformedAssets_Source_Transform");

        builder.HasIndex(e => e.LastAccessedAt)
            .HasDatabaseName("IX_TransformedAssets_LastAccessed");
    }
}
