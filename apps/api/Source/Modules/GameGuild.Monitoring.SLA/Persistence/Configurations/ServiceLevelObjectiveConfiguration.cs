using GameGuild.Monitoring.SLA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA.Persistence.Configurations;

/// <summary>
///     EF Core entity configuration for ServiceLevelObjective.
/// </summary>
public class ServiceLevelObjectiveConfiguration : IEntityTypeConfiguration<ServiceLevelObjective>
{
    public void Configure(EntityTypeBuilder<ServiceLevelObjective> builder)
    {
        builder.HasKey(slo => slo.Id);

        builder.Property(slo => slo.Name).IsRequired().HasMaxLength(200);

        builder.Property(slo => slo.Description).HasMaxLength(1000);

        builder.Property(slo => slo.ServiceName).IsRequired().HasMaxLength(100);

        builder.Property(slo => slo.TargetPercentage).IsRequired().HasPrecision(5, 2);

        builder.Property(slo => slo.TimeWindowDays).IsRequired();

        builder.Property(slo => slo.IsEnabled).IsRequired();

        builder.Property(slo => slo.AlertThresholdPercentage).HasPrecision(5, 2);

        builder.Property(slo => slo.RemainingErrorBudget).HasPrecision(5, 2);

        builder.Property(slo => slo.TenantId).IsRequired(false);

        builder.Property(slo => slo.CreatedAt).IsRequired();

        builder.Property(slo => slo.LastEvaluatedAt);

        // Indexes
        builder.HasIndex(slo => slo.TenantId);
        builder.HasIndex(slo => slo.ServiceName);
        builder.HasIndex(slo => new { slo.TenantId, slo.Name }).IsUnique();
        builder.HasIndex(slo => slo.IsEnabled);

        // Relationships
        builder.HasMany<ServiceLevelIndicator>().WithOne().HasForeignKey(sli => sli.ServiceLevelObjectiveId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<SloViolation>().WithOne().HasForeignKey(v => v.ServiceLevelObjectiveId).OnDelete(DeleteBehavior.Cascade);
    }
}
