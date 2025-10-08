using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for TenantRoleApplication entity
/// </summary>
public class TenantRoleApplicationConfiguration : IEntityTypeConfiguration<TenantRoleApplication>
{
    public void Configure(EntityTypeBuilder<TenantRoleApplication> builder)
    {
        builder.ToTable("TenantRoleApplications");

        builder.HasKey(tra => tra.Id);

        builder.Property(tra => tra.TenantId)
            .IsRequired();

        builder.Property(tra => tra.RoleTemplateId)
            .IsRequired();

        builder.Property(tra => tra.RoleName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tra => tra.CustomDescription)
            .HasMaxLength(500);

        builder.Property(tra => tra.IsActive)
            .HasDefaultValue(true);

        builder.Property(tra => tra.AppliedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure CustomPermissions as JSON column
        builder.Property(tra => tra.CustomPermissions)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<PermissionType[]>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        // Configure Metadata as JSON column
        builder.Property(tra => tra.Metadata)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(tra => new { tra.TenantId, tra.RoleTemplateId })
            .IsUnique()
            .HasDatabaseName("IX_TenantRoleApplications_TenantId_RoleTemplateId");

        builder.HasIndex(tra => tra.RoleName)
            .HasDatabaseName("IX_TenantRoleApplications_RoleName");

        builder.HasIndex(tra => tra.TenantId)
            .HasDatabaseName("IX_TenantRoleApplications_TenantId");

        builder.HasIndex(tra => tra.RoleTemplateId)
            .HasDatabaseName("IX_TenantRoleApplications_RoleTemplateId");

        builder.HasIndex(tra => tra.IsActive)
            .HasDatabaseName("IX_TenantRoleApplications_IsActive");

        // Relationships
        builder.HasOne(tra => tra.Tenant)
            .WithMany()
            .HasForeignKey(tra => tra.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tra => tra.RoleTemplate)
            .WithMany(rt => rt.TenantApplications)
            .HasForeignKey(tra => tra.RoleTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(tra => tra.UserAssignments)
            .WithOne(utr => utr.TenantRoleApplication)
            .HasForeignKey(utr => utr.TenantRoleApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(tra => tra.DeletedAt == null);
    }
}
