
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Entity Type Configuration for ServiceLevelIndicator
/// </summary>
public class ServiceLevelIndicatorConfiguration : IEntityTypeConfiguration<ServiceLevelIndicator>
{
    public void Configure(EntityTypeBuilder<ServiceLevelIndicator> builder)
    {
        // Table configuration
        builder.ToTable("service_level_indicators", "gameguild.sla");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        // Property configurations
        builder.Property(x => x.ServiceLevelObjectiveId).IsRequired();
        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.IsSuccessful).IsRequired();
        builder.Property(x => x.ResponseTimeMs);
        builder.Property(x => x.StatusCode);
        builder.Property(x => x.Endpoint).HasMaxLength(500);
        builder.Property(x => x.Metadata).HasMaxLength(4000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        // Relationships
        builder.HasOne(x => x.ServiceLevelObjective)
            .WithMany(x => x.Indicators)
            .HasForeignKey(x => x.ServiceLevelObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ServiceLevelObjectiveId).HasDatabaseName("ix_sli_slo_id");
        builder.HasIndex(x => x.Timestamp).HasDatabaseName("ix_sli_timestamp");
        builder.HasIndex(x => new { x.ServiceLevelObjectiveId, x.Timestamp }).HasDatabaseName("ix_sli_slo_timestamp");
    }
}
