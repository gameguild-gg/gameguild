using GameGuild.Modules.ErrorTracking.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.ErrorTracking.Configuration;

/// <summary>
/// Entity Framework configuration for ErrorEvent.
/// </summary>
public class ErrorEventConfiguration : IEntityTypeConfiguration<ErrorEvent>
{
    public void Configure(EntityTypeBuilder<ErrorEvent> builder)
    {
        builder.ToTable("ErrorEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired(false);

        builder.Property(e => e.Fingerprint)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.ErrorIssueId)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.ExceptionType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.StackTrace)
            .HasMaxLength(8000);

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Environment)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Release)
            .HasMaxLength(100);

        builder.Property(e => e.UserId)
            .IsRequired(false);

        builder.Property(e => e.Url)
            .HasMaxLength(2000);

        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.Tags)
            .HasColumnType("jsonb");

        builder.Property(e => e.ContextData)
            .HasColumnType("jsonb");

        builder.Property(e => e.Breadcrumbs)
            .HasColumnType("jsonb");

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.Property(e => e.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ResolvedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(e => e.ErrorIssueId);
        builder.HasIndex(e => e.Fingerprint);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.TenantId, e.OccurredAt });
    }
}
