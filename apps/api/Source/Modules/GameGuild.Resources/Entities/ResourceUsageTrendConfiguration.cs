using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources.Entities;

/// <summary>
///     Entity Type Configuration for ResourceUsageTrend
/// </summary>
public class ResourceUsageTrendConfiguration : IEntityTypeConfiguration<ResourceUsageTrend>
{
    public void Configure(EntityTypeBuilder<ResourceUsageTrend> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("resourceusagetrend", "gameguild.resources");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // TODO: Add specific property configurations for ResourceUsageTrend
        // Example:
        // builder.Property(x => x.Name)
        //     .HasColumnName("name")
        //     .HasMaxLength(255)
        //     .IsRequired();

        // TODO: Add relationship configurations
        // Example:
        // builder.HasOne(x => x.Tenant)
        //     .WithMany()
        //     .HasForeignKey(x => x.TenantId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        // builder.HasIndex(x => x.TenantId).HasDatabaseName("idx_resourceusagetrend_tenant_id");

        // Configure created/updated timestamps if inherited from EntityBase
        // builder.Property(x => x.CreatedAt)
        //     .HasColumnName("created_at")
        //     .IsRequired();
        // 
        // builder.Property(x => x.UpdatedAt)
        //     .HasColumnName("updated_at")
        //     .IsRequired();
    }
}
