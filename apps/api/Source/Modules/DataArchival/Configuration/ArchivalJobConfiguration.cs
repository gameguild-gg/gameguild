using GameGuild.Modules.DataArchival.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.DataArchival.Configuration;

/// <summary>
/// Entity Framework configuration for ArchivalJob.
/// </summary>
public class ArchivalJobConfiguration : IEntityTypeConfiguration<ArchivalJob>
{
    public void Configure(EntityTypeBuilder<ArchivalJob> builder)
    {
        builder.ToTable("ArchivalJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.PolicyId)
            .IsRequired();

        builder.Property(j => j.TenantId)
            .IsRequired();

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.StartedAt)
            .IsRequired();

        builder.Property(j => j.CompletedAt)
            .IsRequired(false);

        builder.Property(j => j.ItemsArchived)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(j => j.ItemsDeleted)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(j => j.PolicyId);
        builder.HasIndex(j => j.TenantId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.StartedAt);
        builder.HasIndex(j => new { j.TenantId, j.Status });
        builder.HasIndex(j => new { j.PolicyId, j.Status });
    }
}
