using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity Framework configuration for TenantMetadata
/// </summary>
public class TenantMetadataConfiguration : IEntityTypeConfiguration<TenantMetadata>
{
    public void Configure(EntityTypeBuilder<TenantMetadata> builder)
    {
        // Soft delete query filter
        builder.HasQueryFilter(tm => tm.DeletedAt == null);

        // Configure relationships
        builder.HasOne(tm => tm.Tenant).WithOne().HasForeignKey<TenantMetadata>(tm => tm.TenantId).OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(tm => tm.TenantId).IsUnique();

        builder.HasIndex(tm => tm.Industry);
        builder.HasIndex(tm => tm.Size);
        builder.HasIndex(tm => tm.Type);

        // Configure JSON columns for PostgreSQL
        builder.Property(tm => tm.CustomFields).HasColumnType("jsonb").HasDefaultValue("{}");

        builder.Property(tm => tm.Tags).HasColumnType("jsonb").HasDefaultValue("[]");

        builder.Property(tm => tm.ExternalReferences).HasColumnType("jsonb").HasDefaultValue("{}");

        builder.Property(tm => tm.BusinessInfo).HasColumnType("jsonb").HasDefaultValue("{}");

        builder.Property(tm => tm.ContactInfo).HasColumnType("jsonb").HasDefaultValue("{}");

        // Configure enums
        builder.Property(tm => tm.Size).HasConversion<int>();

        // Configure constraints
        builder.Property(tm => tm.Industry).HasMaxLength(100);

        builder.Property(tm => tm.Type).HasMaxLength(50);

        builder.Property(tm => tm.Notes).HasMaxLength(2000);
    }
}
