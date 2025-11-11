using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Entities;

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

        // Relationships
        builder.HasOne(tm => tm.Tenant).WithMany().HasForeignKey(tm => tm.TenantId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.ParentMember).WithMany(tm => tm.ChildMembers).HasForeignKey(tm => tm.ParentMemberId).OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(tm => new { tm.UserId, tm.TenantId }).IsUnique();
        builder.HasIndex(tm => new { tm.TenantId, tm.IsActive });
        builder.HasIndex(tm => tm.JoinedAt);
        builder.HasIndex(tm => tm.ParentMemberId);
    }
}
