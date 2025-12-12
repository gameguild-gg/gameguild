using GameGuild.Monitoring.SLA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA.Data.Configurations;

/// <summary>
///     Entity Type Configuration for ServiceLevelIndicator
/// </summary>
public class ServiceLevelIndicatorConfiguration : IEntityTypeConfiguration<ServiceLevelIndicator>
{
    public void Configure(EntityTypeBuilder<ServiceLevelIndicator> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).IsRequired();

        // TODO: Add specific property configurations for ServiceLevelIndicator
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
        // builder.HasIndex(x => x.TenantId).HasDatabaseName("idx_servicelevelindicator_tenant_id");

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
