using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Assets.Configuration;

/// <summary>
/// EF Core configuration for AssetReference entity.
/// </summary>
public class AssetReferenceConfiguration : IEntityTypeConfiguration<AssetReference>
{
    public void Configure(EntityTypeBuilder<AssetReference> builder)
    {
        builder.ToTable("asset_references", "assets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AssetContentId)
            .IsRequired();

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.AccessPolicy)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ParentResourceType)
            .HasMaxLength(100);

        builder.Property(e => e.ParentResourceId);

        builder.Property(e => e.FolderId);

        builder.Property(e => e.CurrentRevisionNumber).HasDefaultValue(0);

        builder.Property(e => e.AccessCount)
            .HasDefaultValue(0);

        builder.Property(e => e.LastAccessedAt);

        builder.Property(e => e.DownloadWindowExpiresAt);

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Relationships
        builder.HasOne(e => e.Content)
            .WithMany(content => content.References)
            .HasForeignKey(e => e.AssetContentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.AssetContentId)
            .HasDatabaseName("IX_AssetReferences_ContentId");

        builder.HasIndex(e => e.CreatedByUserId)
            .HasDatabaseName("IX_AssetReferences_UserId");

        builder.HasIndex(e => new { e.ParentResourceType, e.ParentResourceId })
            .HasDatabaseName("IX_AssetReferences_Parent");

        builder.HasIndex(e => e.AccessPolicy)
            .HasDatabaseName("IX_AssetReferences_AccessPolicy");

        builder.HasOne<AssetFolder>()
            .WithMany()
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
