using GameGuild.Modules.ErrorTracking.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.ErrorTracking.Configuration;

/// <summary>
/// Entity Framework configuration for ErrorIssue.
/// </summary>
public class ErrorIssueConfiguration : IEntityTypeConfiguration<ErrorIssue>
{
    public void Configure(EntityTypeBuilder<ErrorIssue> builder)
    {
        builder.ToTable("ErrorIssues");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.TenantId)
            .IsRequired(false);

        builder.Property(i => i.Fingerprint)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.ExceptionType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.EventCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.UserCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.FirstSeenAt)
            .IsRequired();

        builder.Property(i => i.LastSeenAt)
            .IsRequired();

        builder.Property(i => i.Environments)
            .HasMaxLength(500);

        builder.Property(i => i.Releases)
            .HasMaxLength(500);

        builder.Property(i => i.AssignedToUserId)
            .IsRequired(false);

        builder.Property(i => i.ResolvedAt)
            .IsRequired(false);

        builder.Property(i => i.ResolvedByUserId)
            .IsRequired(false);

        builder.Property(i => i.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(i => i.IsMuted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.MutedUntil)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(i => i.Fingerprint);
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => new { i.TenantId, i.Fingerprint }).IsUnique();
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Severity);
        builder.HasIndex(i => i.LastSeenAt);
        builder.HasIndex(i => new { i.TenantId, i.Status, i.LastSeenAt });
    }
}
