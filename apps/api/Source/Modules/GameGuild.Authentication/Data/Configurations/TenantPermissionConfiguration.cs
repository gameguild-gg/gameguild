using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Authentication.Data.Configurations;

/// <summary>
///     Entity Type Configuration for TenantPermission
/// </summary>
public class TenantPermissionConfiguration : IEntityTypeConfiguration<TenantPermission>
{
    public void Configure(EntityTypeBuilder<TenantPermission> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("tenantpermission", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // TODO: Add specific property configurations for TenantPermission
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
        // builder.HasIndex(x => x.TenantId).HasDatabaseName("idx_tenantpermission_tenant_id");

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
