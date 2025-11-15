using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Data.Configurations;

/// <summary>
///     Entity Type Configuration for UsageTracking
/// </summary>
public class UsageTrackingConfiguration : IEntityTypeConfiguration<UsageTracking>
{
    public void Configure(EntityTypeBuilder<UsageTracking> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure TenantId as required (override nullable from base)
        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure relationship to Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for common queries
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
