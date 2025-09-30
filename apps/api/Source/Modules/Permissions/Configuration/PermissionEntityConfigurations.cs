using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Permissions.Configuration;

/// <summary>
/// Entity Framework configuration for TenantPermission with optimized indexes
/// </summary>
public class TenantPermissionConfiguration : IEntityTypeConfiguration<TenantPermission>
{
    public void Configure(EntityTypeBuilder<TenantPermission> builder)
    {
        // Primary composite index for permission lookups (most common query pattern)
        builder.HasIndex(tp => new { tp.UserId, tp.TenantId, tp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_UserTenant_Active")
            .IsUnique(false);

        // Index for default tenant permissions (UserId is null)
        builder.HasIndex(tp => new { tp.TenantId, tp.DeletedAt })
            .HasFilter("user_id IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_TenantDefaults");

        // Index for global default permissions (both UserId and TenantId are null)
        builder.HasIndex(tp => tp.DeletedAt)
            .HasFilter("user_id IS NULL AND tenant_id IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_GlobalDefaults");

        // Index for expiration cleanup and queries
        builder.HasIndex(tp => new { tp.ExpiresAt, tp.DeletedAt })
            .HasFilter("expires_at IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_Expiration");

        // Index for user-centric queries (all permissions for a user)
        builder.HasIndex(tp => new { tp.UserId, tp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_User");

        // Index for tenant-centric queries (all permissions in a tenant)
        builder.HasIndex(tp => new { tp.TenantId, tp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_TenantPermission_Tenant");

        // Covering index for common permission checks (includes computed columns)
        builder.HasIndex(tp => new { tp.UserId, tp.TenantId, tp.ExpiresAt })
            .HasFilter("deleted_at IS NULL")
            .IncludeProperties(tp => new { tp.PermissionFlags1, tp.PermissionFlags2, tp.CreatedAt })
            .HasDatabaseName("IX_TenantPermission_Covering");
    }
}

/// <summary>
/// Entity Framework configuration for ContentTypePermission with optimized indexes
/// </summary>
public class ContentTypePermissionConfiguration : IEntityTypeConfiguration<ContentTypePermission>
{
    public void Configure(EntityTypeBuilder<ContentTypePermission> builder)
    {
        // Primary composite index for content type permission lookups
        builder.HasIndex(ctp => new { ctp.UserId, ctp.TenantId, ctp.ContentType, ctp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_ContentTypePermission_UserTenantType")
            .IsUnique(false);

        // Index for content type defaults (UserId is null)
        builder.HasIndex(ctp => new { ctp.TenantId, ctp.ContentType, ctp.DeletedAt })
            .HasFilter("user_id IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("IX_ContentTypePermission_TypeDefaults");

        // Index for content type queries
        builder.HasIndex(ctp => new { ctp.ContentType, ctp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_ContentTypePermission_ContentType");

        // Index for user permissions across content types
        builder.HasIndex(ctp => new { ctp.UserId, ctp.DeletedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_ContentTypePermission_User");
    }
}

/// <summary>
/// Entity Framework configuration for PermissionAuditLog with optimized indexes
/// </summary>
public class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        // Primary index for audit queries by user and time
        builder.HasIndex(pal => new { pal.UserId, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLog_UserTime")
            .IsDescending(false, true); // UserId ASC, PerformedAt DESC

        // Index for tenant audit queries
        builder.HasIndex(pal => new { pal.TenantId, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLog_TenantTime")
            .IsDescending(false, true);

        // Index for resource audit queries
        builder.HasIndex(pal => new { pal.ResourceId, pal.PerformedAt })
            .HasFilter("resource_id IS NOT NULL")
            .HasDatabaseName("IX_PermissionAuditLog_ResourceTime")
            .IsDescending(false, true);

        // Index for operation-based queries (security monitoring)
        builder.HasIndex(pal => new { pal.Operation, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLog_OperationTime")
            .IsDescending(false, true);

        // Index for failed operations (security analysis)
        builder.HasIndex(pal => new { pal.IsSuccess, pal.PerformedAt })
            .HasFilter("is_success = false")
            .HasDatabaseName("IX_PermissionAuditLog_FailuresTime")
            .IsDescending(false, true);

        // Composite index for performance analytics
        builder.HasIndex(pal => new { pal.TenantId, pal.Operation, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLog_Analytics")
            .IsDescending(false, false, true);

        // Configure JSON columns properly for PostgreSQL
        builder.Property(pal => pal.Permissions)
            .HasColumnType("jsonb");

        builder.Property(pal => pal.Metadata)
            .HasColumnType("jsonb");
    }
}

/// <summary>
/// Entity Framework configuration for PermissionTemplate with optimized indexes
/// </summary>
public class PermissionTemplateConfiguration : IEntityTypeConfiguration<PermissionTemplate>
{
    public void Configure(EntityTypeBuilder<PermissionTemplate> builder)
    {
        // Unique index on template name
        builder.HasIndex(pt => pt.Name)
            .IsUnique()
            .HasDatabaseName("IX_PermissionTemplate_Name_Unique");

        // Index for category-based queries
        builder.HasIndex(pt => new { pt.Category, pt.IsActive })
            .HasDatabaseName("IX_PermissionTemplate_Category");

        // Index for system templates
        builder.HasIndex(pt => new { pt.IsSystemTemplate, pt.IsActive })
            .HasDatabaseName("IX_PermissionTemplate_System");

        // Index for module-specific templates
        builder.HasIndex(pt => new { pt.Module, pt.IsActive })
            .HasFilter("module IS NOT NULL")
            .HasDatabaseName("IX_PermissionTemplate_Module");

        // Configure JSON columns
        builder.Property(pt => pt.Permissions)
            .HasColumnType("jsonb");

        builder.Property(pt => pt.Metadata)
            .HasColumnType("jsonb");
    }
}

/// <summary>
/// Entity Framework configuration for PermissionDelegation with optimized indexes
/// </summary>
public class PermissionDelegationConfiguration : IEntityTypeConfiguration<PermissionDelegation>
{
    public void Configure(EntityTypeBuilder<PermissionDelegation> builder)
    {
        // Primary index for delegate permission lookups
        builder.HasIndex(pd => new { pd.DelegateUserId, pd.TenantId, pd.IsActive, pd.ExpiresAt })
            .HasFilter("is_active = true AND (expires_at IS NULL OR expires_at > NOW())")
            .HasDatabaseName("IX_PermissionDelegation_Delegate_Active");

        // Index for delegator queries (permissions I've delegated)
        builder.HasIndex(pd => new { pd.DelegatorUserId, pd.IsActive })
            .HasDatabaseName("IX_PermissionDelegation_Delegator");

        // Index for tenant delegations
        builder.HasIndex(pd => new { pd.TenantId, pd.IsActive })
            .HasFilter("is_active = true")
            .HasDatabaseName("IX_PermissionDelegation_Tenant");

        // Index for resource-specific delegations
        builder.HasIndex(pd => new { pd.ResourceId, pd.IsActive })
            .HasFilter("resource_id IS NOT NULL AND is_active = true")
            .HasDatabaseName("IX_PermissionDelegation_Resource");

        // Index for expiration cleanup
        builder.HasIndex(pd => new { pd.ExpiresAt, pd.IsActive })
            .HasFilter("expires_at IS NOT NULL AND is_active = true")
            .HasDatabaseName("IX_PermissionDelegation_Expiration");

        // Configure JSON columns
        builder.Property(pd => pd.DelegatedPermissions)
            .HasColumnType("jsonb");

        builder.Property(pd => pd.Conditions)
            .HasColumnType("jsonb");
    }
}