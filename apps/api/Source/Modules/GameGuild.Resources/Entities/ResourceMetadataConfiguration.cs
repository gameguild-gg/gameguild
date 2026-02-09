using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     EF Core configuration for ResourceMetadata entity
/// </summary>
public class ResourceMetadataConfiguration : IEntityTypeConfiguration<ResourceMetadata>
{
    public void Configure(EntityTypeBuilder<ResourceMetadata> builder)
    {
        builder.ToTable("resource_metadata");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Category });
        builder.HasIndex(e => new { e.UserId, e.Key });

        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Value).HasMaxLength(4000);
        builder.Property(e => e.DataType).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Category).HasMaxLength(100);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
