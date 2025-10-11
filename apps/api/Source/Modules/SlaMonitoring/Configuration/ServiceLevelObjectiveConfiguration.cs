using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Configuration;

/// <summary>
/// EF Core entity configuration for ServiceLevelObjective.
/// </summary>
public class ServiceLevelObjectiveConfiguration : IEntityTypeConfiguration<ServiceLevelObjective>
{
    public void Configure(EntityTypeBuilder<ServiceLevelObjective> builder)
    {
        builder.ToTable("ServiceLevelObjectives");

        builder.HasKey(slo => slo.Id);

        builder.Property(slo => slo.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(slo => slo.Description)
            .HasMaxLength(1000);

        builder.Property(slo => slo.ServiceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(slo => slo.TargetPercentage)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(slo => slo.TimeWindowDays)
            .IsRequired();

        builder.Property(slo => slo.ErrorBudgetPercentage)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(slo => slo.AlertThresholdPercentage)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(slo => slo.IsActive)
            .IsRequired();

        builder.Property(slo => slo.CurrentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(slo => slo.TenantId);

        builder.Property(slo => slo.CreatedAt)
            .IsRequired();

        builder.Property(slo => slo.UpdatedAt)
            .IsRequired();

        builder.Property(slo => slo.LastEvaluatedAt);

        builder.HasIndex(slo => slo.TenantId);
        builder.HasIndex(slo => slo.ServiceName);
        builder.HasIndex(slo => slo.IsActive);
        builder.HasIndex(slo => slo.CurrentStatus);
    }
}
