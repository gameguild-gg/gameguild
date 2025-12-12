using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Data.Configurations;

/// <summary>
///     Entity Type Configuration for TenantStatistics
/// </summary>
public class TenantStatisticsConfiguration : IEntityTypeConfiguration<TenantStatistics>
{
    public void Configure(EntityTypeBuilder<TenantStatistics> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure TenantId as required (override nullable from base)
        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure relationship to Tenant (one-to-one)
        builder.HasOne(x => x.Tenant)
            .WithOne()
            .HasForeignKey<TenantStatistics>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure index on TenantId
        builder.HasIndex(x => x.TenantId)
            .IsUnique();
    }
}
