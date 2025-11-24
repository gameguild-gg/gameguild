using GameGuild.Monitoring.SLA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA.Persistence.Configurations;

/// <summary>
///     EF Core entity configuration for SloViolation.
/// </summary>
public class SloViolationConfiguration : IEntityTypeConfiguration<SloViolation>
{
    public void Configure(EntityTypeBuilder<SloViolation> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ServiceLevelObjectiveId).IsRequired();

        builder.Property(v => v.Severity).IsRequired().HasConversion<string>();

        builder.Property(v => v.StartedAt).IsRequired();

        builder.Property(v => v.EndedAt);

        builder.Property(v => v.ActualValue).IsRequired().HasPrecision(5, 2);

        builder.Property(v => v.TargetValue).IsRequired().HasPrecision(5, 2);

        builder.Property(v => v.Description).HasMaxLength(2000);

        builder.Property(v => v.Notes).HasMaxLength(2000);

        builder.Property(v => v.TenantId).IsRequired(false);

        // Indexes
        builder.HasIndex(v => v.ServiceLevelObjectiveId);
        builder.HasIndex(v => v.StartedAt);
        builder.HasIndex(v => v.EndedAt);
        builder.HasIndex(v => v.Severity);
        builder.HasIndex(v => v.TenantId);
        builder.HasIndex(v => new { v.ServiceLevelObjectiveId, v.EndedAt });
    }
}
