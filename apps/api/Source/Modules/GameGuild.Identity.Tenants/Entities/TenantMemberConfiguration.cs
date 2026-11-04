using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity Framework configuration for TenantMember entity
/// </summary>
public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(tm => tm.Id);

        // Properties
        builder.Property(tm => tm.UserId).IsRequired();
        builder.Property(tm => tm.TenantId).IsRequired();
        builder.Property(tm => tm.Role).IsRequired().HasMaxLength(100);
        builder.Property(tm => tm.IsActive).IsRequired();
        builder.Property(tm => tm.JoinedAt).IsRequired();
        builder.Property(tm => tm.LeftAt).IsRequired(false);
        builder.Property(tm => tm.LeaveReason).HasMaxLength(500).IsRequired(false);
        builder.Property(tm => tm.Metadata).IsRequired(false);
        builder.Property(tm => tm.ParentMemberId).IsRequired(false);

        // Soft delete query filter
        builder.HasQueryFilter(tm => tm.DeletedAt == null);

        // Relationships
        builder.HasOne(tm => tm.Tenant).WithMany(t => t.TenantMembers).HasForeignKey(tm => tm.TenantId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.ParentMember).WithMany(tm => tm.ChildMembers).HasForeignKey(tm => tm.ParentMemberId).OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(tm => new { tm.UserId, tm.TenantId }).IsUnique();
        // Issue #1157: membership resolution and roster listing run on every sign-in and
        // tenant switch, and the global soft-delete filter is applied to both; the partial
        // indexes keep deleted memberships out of those hot paths entirely.
        builder.HasIndex(tm => tm.UserId)
            .HasDatabaseName("ix_tenant_members_user_id_active")
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(tm => new { tm.TenantId, tm.IsActive })
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(tm => tm.JoinedAt);
        builder.HasIndex(tm => tm.ParentMemberId);
    }
}
