using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Assets.Configuration;

public sealed class AssetFolderConfiguration : IEntityTypeConfiguration<AssetFolder>
{
    public void Configure(EntityTypeBuilder<AssetFolder> builder)
    {
        builder.ToTable("asset_folders", "assets");
        builder.HasKey(folder => folder.Id);
        builder.Property(folder => folder.ParentResourceType).HasMaxLength(100).IsRequired();
        builder.Property(folder => folder.Name).HasMaxLength(255).IsRequired();
        builder.Property(folder => folder.RestrictionMode).HasConversion<string>().HasMaxLength(50);
        builder.Property(folder => folder.AllowedTeamIdsJson).HasMaxLength(4000);
        builder.Property(folder => folder.AllowedAuthoritiesJson).HasMaxLength(2000);
        builder.Ignore(folder => folder.AllowedTeamIds);
        builder.Ignore(folder => folder.AllowedAuthorities);
        builder.HasIndex(folder => new { folder.ParentResourceType, folder.ParentResourceId, folder.ParentFolderId, folder.Name })
            .IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasQueryFilter(folder => folder.DeletedAt == null);
    }
}

public sealed class AssetReferenceRevisionConfiguration : IEntityTypeConfiguration<AssetReferenceRevision>
{
    public void Configure(EntityTypeBuilder<AssetReferenceRevision> builder)
    {
        builder.ToTable("asset_reference_revisions", "assets");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Note).HasMaxLength(500);
        builder.HasIndex(revision => new { revision.AssetReferenceId, revision.RevisionNumber }).IsUnique();
        builder.HasOne(revision => revision.Reference).WithMany(reference => reference.Revisions)
            .HasForeignKey(revision => revision.AssetReferenceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(revision => revision.Content).WithMany()
            .HasForeignKey(revision => revision.AssetContentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(revision => revision.DeletedAt == null);
    }
}

public sealed class AssetScopedAccessGrantConfiguration : IEntityTypeConfiguration<AssetScopedAccessGrant>
{
    public void Configure(EntityTypeBuilder<AssetScopedAccessGrant> builder)
    {
        builder.ToTable("asset_scoped_access_grants", "assets");
        builder.HasKey(grant => grant.Id);
        builder.Property(grant => grant.ScopeType).HasMaxLength(100).IsRequired();
        builder.HasIndex(grant => new { grant.AssetReferenceId, grant.UserId, grant.ScopeType, grant.ScopeId });
        builder.HasIndex(grant => grant.ExpiresAt);
        builder.HasOne<AssetReference>().WithMany().HasForeignKey(grant => grant.AssetReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(grant => grant.IsActive);
        builder.HasQueryFilter(grant => grant.DeletedAt == null);
    }
}
