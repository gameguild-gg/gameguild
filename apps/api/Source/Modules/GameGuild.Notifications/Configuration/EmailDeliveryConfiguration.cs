using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Notifications.Configuration;

/// <summary>
/// EF Core configuration for the EmailDeliveryEvent entity
/// </summary>
public class EmailDeliveryEventConfiguration : IEntityTypeConfiguration<EmailDeliveryEvent>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderMessageId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.RecipientEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.EventType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.BounceType)
            .HasMaxLength(30);

        builder.Property(e => e.DiagnosticCode)
            .HasMaxLength(200);

        builder.Property(e => e.SnsMessageId)
            .IsRequired()
            .HasMaxLength(100);

        // No MaxLength on purpose: length is enforced in code (webhook truncates to 4000
        // chars before save); jsonb has no varchar length semantics.
        builder.Property(e => e.Payload)
            .HasColumnType("jsonb");

        builder.HasIndex(e => e.ProviderMessageId);

        builder.HasIndex(e => e.SnsMessageId)
            .IsUnique();

        // Global query filter for soft delete
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}

/// <summary>
/// EF Core configuration for the EmailSuppression entity
/// </summary>
public class EmailSuppressionConfiguration : IEntityTypeConfiguration<EmailSuppression>
{
    public void Configure(EntityTypeBuilder<EmailSuppression> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmailAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(s => s.Reason)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.BounceType)
            .HasMaxLength(30);

        // One row per address: re-suppression upserts into the existing row
        builder.HasIndex(s => s.EmailAddress)
            .IsUnique();

        // Global query filter for soft delete
        builder.HasQueryFilter(s => s.DeletedAt == null);
    }
}
