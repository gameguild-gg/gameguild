
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Entity Type Configuration for SloViolation
/// </summary>
public class SloViolationConfiguration : IEntityTypeConfiguration<SloViolation>
{
    public void Configure(EntityTypeBuilder<SloViolation> builder)
    {
        // Table configuration
        builder.ToTable("slo_violations", "gameguild.sla");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        // Property configurations
        builder.Property(x => x.ServiceLevelObjectiveId).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.EndedAt);
        builder.Property(x => x.ActualValue).IsRequired();
        builder.Property(x => x.TargetValue).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AlertTriggered).IsRequired();
        builder.Property(x => x.AlertSentAt);
        builder.Property(x => x.IsAcknowledged).IsRequired();
        builder.Property(x => x.AcknowledgedByUserId);
        builder.Property(x => x.AcknowledgedAt);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.Description).HasMaxLength(2000);

        // Relationships
        builder.HasOne(x => x.ServiceLevelObjective)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.ServiceLevelObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ServiceLevelObjectiveId).HasDatabaseName("ix_sloviolation_slo_id");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName("ix_sloviolation_started_at");
        builder.HasIndex(x => x.Severity).HasDatabaseName("ix_sloviolation_severity");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_sloviolation_tenant_id");
    }
}
