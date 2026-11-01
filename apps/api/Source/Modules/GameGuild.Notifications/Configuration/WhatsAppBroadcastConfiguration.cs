using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Notifications.Configuration;

/// <summary>
/// EF Core configuration for the WhatsAppBroadcast entity (issue #55)
/// </summary>
public class WhatsAppBroadcastConfiguration : IEntityTypeConfiguration<WhatsAppBroadcast>
{
    public void Configure(EntityTypeBuilder<WhatsAppBroadcast> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.TemplateName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.LanguageCode)
            .HasMaxLength(10);

        builder.Property(b => b.VariablesJson)
            .HasMaxLength(4000);

        builder.Property(b => b.SegmentRole)
            .HasMaxLength(100);

        builder.Property(b => b.Error)
            .HasMaxLength(1000);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany<WhatsAppBroadcastRecipient>()
            .WithOne()
            .HasForeignKey(r => r.BroadcastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => b.DeletedAt == null);
    }
}

/// <summary>
/// EF Core configuration for the WhatsAppBroadcastRecipient entity (issue #55)
/// </summary>
public class WhatsAppBroadcastRecipientConfiguration : IEntityTypeConfiguration<WhatsAppBroadcastRecipient>
{
    public void Configure(EntityTypeBuilder<WhatsAppBroadcastRecipient> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.ProviderMessageId)
            .HasMaxLength(100);

        builder.Property(r => r.Error)
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
