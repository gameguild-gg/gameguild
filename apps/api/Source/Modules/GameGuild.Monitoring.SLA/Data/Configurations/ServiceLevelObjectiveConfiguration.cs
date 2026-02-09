
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Entity Type Configuration for ServiceLevelObjective
/// </summary>
public class ServiceLevelObjectiveConfiguration : IEntityTypeConfiguration<ServiceLevelObjective>
{
    public void Configure(EntityTypeBuilder<ServiceLevelObjective> builder)
    {
        // Table configuration
        builder.ToTable("service_level_objectives", "gameguild.sla");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        // Property configurations
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetPercentage).IsRequired();
        builder.Property(x => x.TimeWindowDays).IsRequired().HasDefaultValue(30);
        builder.Property(x => x.ErrorBudgetPercentage).IsRequired();
        builder.Property(x => x.AlertThresholdPercentage).IsRequired().HasDefaultValue(50.0);
        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastEvaluatedAt);
        builder.Property(x => x.CurrentActualPercentage);
        builder.Property(x => x.RemainingErrorBudget);

        // Relationships (configured on the child side)
        builder.HasMany(x => x.Indicators)
            .WithOne(x => x.ServiceLevelObjective)
            .HasForeignKey(x => x.ServiceLevelObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Violations)
            .WithOne(x => x.ServiceLevelObjective)
            .HasForeignKey(x => x.ServiceLevelObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_slo_tenant_id");
        builder.HasIndex(x => x.ServiceName).HasDatabaseName("ix_slo_service_name");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_slo_status");
        builder.HasIndex(x => x.IsEnabled).HasDatabaseName("ix_slo_is_enabled");
    }
}
