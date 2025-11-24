using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Authentication.Database.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Role entity.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("role", "gameguild.authentication");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(r => r.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(r => r.IsGlobal)
            .HasColumnName("is_global")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(r => r.Name)
            .HasDatabaseName("idx_role_name");

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("idx_role_tenant_id");

        builder.HasIndex(r => new { r.Name, r.TenantId })
            .IsUnique()
            .HasDatabaseName("idx_role_name_tenant_id");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("idx_role_is_active");
    }
}
