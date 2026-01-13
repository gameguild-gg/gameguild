using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authorization.Configuration;

/// <summary>
///     EF Core configuration for DynamicRole entity.
/// </summary>
public class DynamicRoleConfiguration : IEntityTypeConfiguration<DynamicRole>
{
    public void Configure(EntityTypeBuilder<DynamicRole> builder)
    {
        builder.ToTable("DynamicRoles");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.ParentRoleId);
        
        // Properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(e => e.Description)
            .HasMaxLength(2000);
        
        // PostgreSQL native array support for Permissions
        builder.Property(e => e.Permissions)
            .HasColumnType("text[]")
            .IsRequired();
        
        // PostgreSQL native array support for DenyPermissions
        builder.Property(e => e.DenyPermissions)
            .HasColumnType("text[]")
            .IsRequired();
        
        // PostgreSQL native array for mutually exclusive role IDs
        builder.Property(e => e.MutuallyExclusiveRoleIds)
            .HasColumnType("uuid[]")
            .IsRequired();
        
        // PostgreSQL native array for prerequisite role IDs
        builder.Property(e => e.PrerequisiteRoleIds)
            .HasColumnType("uuid[]")
            .IsRequired();
        
        // Metadata as JSONB
        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");
        
        // Self-referencing relationship for role hierarchy
        builder.HasOne(e => e.ParentRole)
            .WithMany(e => e.ChildRoles)
            .HasForeignKey(e => e.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
///     EF Core configuration for DynamicRoleAssignment entity.
/// </summary>
public class DynamicRoleAssignmentConfiguration : IEntityTypeConfiguration<DynamicRoleAssignment>
{
    public void Configure(EntityTypeBuilder<DynamicRoleAssignment> builder)
    {
        builder.ToTable("DynamicRoleAssignments");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.RoleId);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => new { e.UserId, e.RoleId, e.TenantId }).IsUnique();
        builder.HasIndex(e => new { e.StartsAt, e.ExpiresAt });
        
        // Properties
        builder.Property(e => e.Reason)
            .HasMaxLength(2000);
        
        // Relationship to DynamicRole
        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
