using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Data.Configurations;

/// <summary>
///     Entity Type Configuration for TenantMember
/// </summary>
public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure TenantId as required (override nullable from base)
        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure UserId as required
        builder.Property(x => x.UserId)
            .IsRequired();

        // Configure relationship to Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure self-referencing relationship for hierarchy
        builder.HasOne(x => x.ParentMember)
            .WithMany(x => x.ChildMembers)
            .HasForeignKey(x => x.ParentMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string properties
        builder.Property(x => x.Role)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LeaveReason)
            .HasMaxLength(500);

        // Configure indexes
        builder.HasIndex(x => new { x.UserId, x.TenantId })
            .IsUnique();
        
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
        builder.HasIndex(x => x.JoinedAt);
        builder.HasIndex(x => x.ParentMemberId);
    }
}
