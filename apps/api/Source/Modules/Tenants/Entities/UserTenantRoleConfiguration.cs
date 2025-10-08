using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for UserTenantRole entity
/// </summary>
public class UserTenantRoleConfiguration : IEntityTypeConfiguration<UserTenantRole>
{
    public void Configure(EntityTypeBuilder<UserTenantRole> builder)
    {
        builder.ToTable("UserTenantRoles");

        builder.HasKey(utr => utr.Id);

        builder.Property(utr => utr.UserId)
            .IsRequired();

        builder.Property(utr => utr.TenantId)
            .IsRequired();

        builder.Property(utr => utr.TenantRoleApplicationId)
            .IsRequired();

        builder.Property(utr => utr.IsActive)
            .HasDefaultValue(true);

        builder.Property(utr => utr.AssignedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure CustomPermissions as JSON column
        builder.Property(utr => utr.CustomPermissions)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<PermissionType[]>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        // Configure Metadata as JSON column
        builder.Property(utr => utr.Metadata)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(utr => new { utr.UserId, utr.TenantId, utr.TenantRoleApplicationId })
            .IsUnique()
            .HasDatabaseName("IX_UserTenantRoles_User_Tenant_Role");

        builder.HasIndex(utr => utr.TenantId)
            .HasDatabaseName("IX_UserTenantRoles_TenantId");

        builder.HasIndex(utr => utr.UserId)
            .HasDatabaseName("IX_UserTenantRoles_UserId");

        builder.HasIndex(utr => utr.TenantRoleApplicationId)
            .HasDatabaseName("IX_UserTenantRoles_TenantRoleApplicationId");

        builder.HasIndex(utr => utr.IsActive)
            .HasDatabaseName("IX_UserTenantRoles_IsActive");

        builder.HasIndex(utr => utr.ExpiresAt)
            .HasDatabaseName("IX_UserTenantRoles_ExpiresAt");

        // Relationships
        builder.HasOne(utr => utr.User)
            .WithMany()
            .HasForeignKey(utr => utr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(utr => utr.Tenant)
            .WithMany()
            .HasForeignKey(utr => utr.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(utr => utr.TenantRoleApplication)
            .WithMany(tra => tra.UserAssignments)
            .HasForeignKey(utr => utr.TenantRoleApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(utr => utr.DeletedAt == null);
    }
}
