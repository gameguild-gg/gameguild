using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Configuration;

/// <summary>
/// EF Core entity configuration for ServiceLevelIndicator.
/// </summary>
public class ServiceLevelIndicatorConfiguration : IEntityTypeConfiguration<ServiceLevelIndicator>
{
    public void Configure(EntityTypeBuilder<ServiceLevelIndicator> builder)
    {
        builder.ToTable("ServiceLevelIndicators");

        builder.HasKey(sli => sli.Id);

        builder.Property(sli => sli.SloId)
            .IsRequired();

        builder.Property(sli => sli.MetricValue)
            .IsRequired();

        builder.Property(sli => sli.IsSuccessful)
            .IsRequired();

        builder.Property(sli => sli.Timestamp)
            .IsRequired();

        builder.Property(sli => sli.Metadata)
            .HasColumnType("jsonb");

        builder.HasOne(sli => sli.ServiceLevelObjective)
            .WithMany()
            .HasForeignKey(sli => sli.SloId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sli => sli.SloId);
        builder.HasIndex(sli => sli.Timestamp);
        builder.HasIndex(sli => sli.IsSuccessful);
    }
}
