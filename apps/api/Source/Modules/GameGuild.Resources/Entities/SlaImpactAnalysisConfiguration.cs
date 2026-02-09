using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for SlaImpactAnalysis
/// </summary>
public class SlaImpactAnalysisConfiguration : IEntityTypeConfiguration<SlaImpactAnalysis>
{
    public void Configure(EntityTypeBuilder<SlaImpactAnalysis> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("SlaImpactAnalyses", "gameguild.resources");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.ResourceQuotaId).IsRequired();
        builder.Property(x => x.UserId);
        builder.Property(x => x.ViolationStartTime).IsRequired();
        builder.Property(x => x.ViolationEndTime);
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ViolationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExpectedValue).IsRequired();
        builder.Property(x => x.ActualValue).IsRequired();
        builder.Property(x => x.DeviationPercentage).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.BusinessImpact).HasMaxLength(500);
        builder.Property(x => x.RootCause).HasMaxLength(1000);
        builder.Property(x => x.MitigationActions).HasMaxLength(1000);
        builder.Property(x => x.IsResolved).IsRequired();
        builder.Property(x => x.ResolvedAt);
        builder.Property(x => x.ResolvedByUserId);
        builder.Property(x => x.RequiresEscalation).IsRequired();
        builder.Property(x => x.IncidentCreated).IsRequired();
        builder.Property(x => x.IncidentTicketId).HasMaxLength(100);
        builder.Property(x => x.Metadata).HasMaxLength(2000);

        // Relationships
        builder.HasOne(x => x.ResourceQuota)
            .WithMany()
            .HasForeignKey(x => x.ResourceQuotaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_slaimpactanalysis_tenant_id");
        builder.HasIndex(x => x.ResourceQuotaId).HasDatabaseName("ix_slaimpactanalysis_quota_id");
        builder.HasIndex(x => x.Severity).HasDatabaseName("ix_slaimpactanalysis_severity");
        builder.HasIndex(x => x.ViolationStartTime).HasDatabaseName("ix_slaimpactanalysis_start_time");
        builder.HasIndex(x => x.IsResolved).HasDatabaseName("ix_slaimpactanalysis_is_resolved");
    }
}
