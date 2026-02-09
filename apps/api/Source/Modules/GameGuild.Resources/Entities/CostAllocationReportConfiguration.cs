using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for CostAllocationReport
/// </summary>
public class CostAllocationReportConfiguration : IEntityTypeConfiguration<CostAllocationReport>
{
    public void Configure(EntityTypeBuilder<CostAllocationReport> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("cost_allocation_reports", "gameguild.resources");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.ResourceUsageType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.TotalUsage).IsRequired();
        builder.Property(x => x.CostPerUnit).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.AllocationTags).HasMaxLength(2000);
        builder.Property(x => x.CostCenter).HasMaxLength(200);
        builder.Property(x => x.Project).HasMaxLength(200);
        builder.Property(x => x.Owner).HasMaxLength(200);
        builder.Property(x => x.IsExported).IsRequired();
        builder.Property(x => x.ExportedAt);
        builder.Property(x => x.InvoiceReference).HasMaxLength(100);
        builder.Property(x => x.Metadata).HasMaxLength(2000);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_costallocationreport_tenant_id");
        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).HasDatabaseName("ix_costallocationreport_period");
        builder.HasIndex(x => x.ResourceUsageType).HasDatabaseName("ix_costallocationreport_type");
    }
}
