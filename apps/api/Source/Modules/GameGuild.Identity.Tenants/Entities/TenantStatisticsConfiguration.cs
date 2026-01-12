using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity Framework configuration for TenantStatistics entity
/// </summary>
public class TenantStatisticsConfiguration : IEntityTypeConfiguration<TenantStatistics>
{
    public void Configure(EntityTypeBuilder<TenantStatistics> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(ts => ts.Id);

        // Properties
        builder.Property(tst => tst.TenantId).IsRequired();
        builder.Property(tst => tst.StatisticDate).IsRequired();
        builder.Property(tst => tst.TotalMembers).IsRequired();
        builder.Property(tst => tst.ActiveMembers).IsRequired();
        builder.Property(tst => tst.InactiveMembers).IsRequired();
        builder.Property(tst => tst.StorageUsed).IsRequired();
        builder.Property(tst => tst.ApiCalls).IsRequired();
        builder.Property(tst => tst.NewMembers).IsRequired();
        builder.Property(tst => tst.MembersLeft).IsRequired();
        builder.Property(tst => tst.CustomMetrics).IsRequired(false);

        // Soft delete query filter
        builder.HasQueryFilter(tst => tst.DeletedAt == null);

        // Relationships
        builder.HasOne(tst => tst.Tenant).WithOne(t => t.TenantStatistics).HasForeignKey<TenantStatistics>(tst => tst.TenantId).OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(tst => tst.TenantId);
        builder.HasIndex(tst => tst.StatisticDate);
    }
}
