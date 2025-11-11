using GameGuild.Features.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features.Data.Configurations;

/// <summary>
///     Entity Type Configuration for FeatureFlag
/// </summary>
public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        // Configure table name (snake_case convention)

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).IsRequired();

        // TODO: Add specific property configurations for FeatureFlag
        // Example:
        // builder.Property(x => x.Name)
        //     
        //     .HasMaxLength(255)
        //     .IsRequired();

        // TODO: Add relationship configurations
        // Example:
        // builder.HasOne(x => x.Tenant)
        //     .WithMany()
        //     .HasForeignKey(x => x.TenantId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        // builder.HasIndex(x => x.TenantId).HasDatabaseName("idx_featureflag_tenant_id");

        // Configure created/updated timestamps if inherited from EntityBase
        // builder.Property(x => x.CreatedAt)
        //     
        //     .IsRequired();
        // 
        // builder.Property(x => x.UpdatedAt)
        //     
        //     .IsRequired();
    }
}
