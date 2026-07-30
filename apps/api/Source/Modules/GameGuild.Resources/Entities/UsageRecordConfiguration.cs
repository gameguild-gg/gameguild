using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for UsageRecord
/// </summary>
public class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
{
    public void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        // Configure table name
        builder.ToTable("usage_records", "resources");

        // Configure primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired().HasComment("Unique identifier for the usage record");

        // Configure foreign keys
        builder.Property(x => x.TenantId).IsRequired().HasComment("Tenant that used the resource");

        builder.Property(x => x.ResourceQuotaId).IsRequired().HasComment("Associated resource quota");

        // Configure resource usage details
        builder.Property(x => x.Type).IsRequired().HasComment("Type of resource used");

        builder.Property(x => x.Count).IsRequired().HasColumnType("bigint").HasComment("Amount of resource consumed");

        // Configure usage period
        builder.Property(x => x.PeriodStart).IsRequired().HasComment("When the usage period started");

        builder.Property(x => x.PeriodEnd).IsRequired().HasComment("When the usage period ended");

        // Configure usage metrics
        builder.Property(x => x.AveragePerDay).HasComment("Average usage per day");

        builder.Property(x => x.PeakUsage).HasComment("Peak usage during period");

        builder.Property(x => x.PeakUsageDate).HasComment("When peak usage occurred");

        // Configure metadata
        builder.Property(x => x.Metadata).HasColumnType("jsonb").HasComment("Additional metadata in JSON format");

        // Configure audit fields
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure relationships - Note: ResourceQuota navigation property not currently in entity

        // Configure indexes for efficient querying
        builder.HasIndex(x => new { x.TenantId, x.Type, x.PeriodStart }).HasDatabaseName("IX_UsageRecords_Tenant_Resource_Time");

        builder.HasIndex(x => x.PeriodStart).HasDatabaseName("IX_UsageRecords_PeriodStart");

        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).HasDatabaseName("IX_UsageRecords_UsagePeriod");

        // Configure check constraints using ToTable
        builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_UsageRecord_Count_NonNegative", "\"Count\" >= 0");
                t.HasCheckConstraint("CK_UsageRecord_PeriodOrder", "\"PeriodEnd\" >= \"PeriodStart\"");
                t.HasCheckConstraint("CK_UsageRecord_PeakUsage_NonNegative", "\"PeakUsage\" IS NULL OR \"PeakUsage\" >= 0");
            }
        );
    }
}
