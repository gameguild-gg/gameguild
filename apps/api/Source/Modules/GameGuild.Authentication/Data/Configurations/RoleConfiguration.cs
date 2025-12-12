using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Authentication.Data.Configurations;

/// <summary>
///     Entity Type Configuration for Role
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("role", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Configure Name property
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Configure Description property
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        // Configure Permissions property (JSON)
        builder.Property(x => x.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("jsonb")
            .IsRequired();

        // Configure IsActive property
        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Configure TenantId property
        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired(false);

        // Configure timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Configure indexes
        builder.HasIndex(x => x.Name)
            .HasDatabaseName("idx_role_name");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("idx_role_tenant_id");

        builder.HasIndex(x => new { x.Name, x.TenantId })
            .HasDatabaseName("idx_role_name_tenant_id")
            .IsUnique();

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("idx_role_is_active");
    }
}
