using GameGuild.Monitoring.SLA.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Monitoring.SLA.Persistence.Configurations;

/// <summary>
///     EF Core entity configuration for ServiceLevelIndicator (SLI metrics).
/// </summary>
public class ServiceLevelIndicatorConfiguration : IEntityTypeConfiguration<ServiceLevelIndicator>
{
    public void Configure(EntityTypeBuilder<ServiceLevelIndicator> builder)
    {
        builder.HasKey(sli => sli.Id);

        builder.Property(sli => sli.ServiceLevelObjectiveId).IsRequired();

        builder.Property(sli => sli.Timestamp).IsRequired();

        builder.Property(sli => sli.IsSuccessful).IsRequired();

        builder.Property(sli => sli.Value).IsRequired().HasPrecision(18, 6);

        builder.Property(sli => sli.ErrorMessage).HasMaxLength(2000);

        builder.Property(sli => sli.ResponseTimeMs);

        builder.Property(sli => sli.StatusCode);

        builder.Property(sli => sli.Endpoint).HasMaxLength(500);

        builder.Property(sli => sli.Metadata).HasMaxLength(4000);

        builder.Property(sli => sli.TenantId).IsRequired(false);

        // Indexes
        builder.HasIndex(sli => sli.ServiceLevelObjectiveId);
        builder.HasIndex(sli => sli.Timestamp);
        builder.HasIndex(sli => new { sli.ServiceLevelObjectiveId, sli.Timestamp });
        builder.HasIndex(sli => sli.IsSuccessful);
        builder.HasIndex(sli => sli.TenantId);
    }
}
