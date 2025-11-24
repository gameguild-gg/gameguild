using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources.Entities;

/// <summary>
///     Entity Type Configuration for ResourceQuota
/// </summary>
public class ResourceQuotaConfiguration : IEntityTypeConfiguration<ResourceQuota>
{
    public void Configure(EntityTypeBuilder<ResourceQuota> builder)
    {
        // Configure table name
        builder.ToTable("resource_quotas", "resources");

        // Configure primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired().HasComment("Unique identifier for the resource quota");

        // Configure TenantId
        builder.Property(x => x.TenantId).IsRequired().HasComment("Tenant that owns this quota");

        // Configure Type (enum)
        builder.Property(x => x.Type).IsRequired().HasComment("Type of resource being limited");

        // Configure quota properties
        builder.Property(x => x.SoftLimit).HasColumnType("bigint").HasComment("Soft limit (warning threshold)");

        builder.Property(x => x.HardLimit).HasColumnType("bigint").HasComment("Hard limit (enforcement threshold)");

        builder.Property(x => x.CurrentUsage).IsRequired().HasColumnType("bigint").HasDefaultValue(0).HasComment("Current usage amount");

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true).HasComment("Whether this quota is actively enforced");

        builder.Property(x => x.Period).IsRequired().HasComment("Period type for quota reset");

        // Configure audit fields
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasComment("When the quota was created");

        builder.Property(x => x.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasComment("When the quota was last updated");

        // Configure indexes
        builder.HasIndex(x => new { x.TenantId, x.Type }).IsUnique().HasDatabaseName("IX_ResourceQuotas_TenantId_ResourceType");

        builder.HasIndex(x => x.Type).HasDatabaseName("IX_ResourceQuotas_ResourceType");

        // Configure check constraints using ToTable
        builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ResourceQuota_MaxUsage_NonNegative", "\"HardLimit\" IS NULL OR \"HardLimit\" >= 0");
                t.HasCheckConstraint("CK_ResourceQuota_CurrentUsage_NonNegative", "\"CurrentUsage\" >= 0");
                t.HasCheckConstraint("CK_ResourceQuota_CurrentUsage_LessEqual_MaxUsage", "\"HardLimit\" IS NULL OR \"CurrentUsage\" <= \"HardLimit\"");
            }
        );
    }
}
