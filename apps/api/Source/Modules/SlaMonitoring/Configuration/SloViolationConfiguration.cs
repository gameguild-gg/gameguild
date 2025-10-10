using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Configuration;

/// <summary>
/// EF Core entity configuration for SloViolation.
/// </summary>
public class SloViolationConfiguration : IEntityTypeConfiguration<SloViolation>
{
    public void Configure(EntityTypeBuilder<SloViolation> builder)
    {
        builder.ToTable("SloViolations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SloId)
            .IsRequired();

        builder.Property(v => v.TenantId);

        builder.Property(v => v.StartedAt)
            .IsRequired();

        builder.Property(v => v.EndedAt);

        builder.Property(v => v.ActualValue)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(v => v.TargetValue)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(v => v.Severity)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Property(v => v.IsResolved)
            .IsRequired();

        builder.HasOne(v => v.ServiceLevelObjective)
            .WithMany()
            .HasForeignKey(v => v.SloId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.SloId);
        builder.HasIndex(v => v.TenantId);
        builder.HasIndex(v => v.StartedAt);
        builder.HasIndex(v => v.IsResolved);
        builder.HasIndex(v => v.Severity);
    }
}
