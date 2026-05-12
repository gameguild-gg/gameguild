using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Content.Pages;

/// <summary>EF Core configuration for <see cref="MarketingLead"/>.</summary>
public class MarketingLeadConfiguration : IEntityTypeConfiguration<MarketingLead>
{
    public void Configure(EntityTypeBuilder<MarketingLead> builder)
    {
        builder.Property(lead => lead.Source)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(lead => lead.Status)
            .HasMaxLength(40)
            .HasDefaultValue(MarketingLeadStatuses.New)
            .IsRequired();

        builder.Property(lead => lead.Name).HasMaxLength(120);
        builder.Property(lead => lead.Email).HasMaxLength(200).IsRequired();
        builder.Property(lead => lead.Company).HasMaxLength(200);
        builder.Property(lead => lead.Topic).HasMaxLength(40);
        builder.Property(lead => lead.Plan).HasMaxLength(60);
        builder.Property(lead => lead.Message).HasMaxLength(4000);
        builder.Property(lead => lead.Locale).HasMaxLength(10);
        builder.Property(lead => lead.PagePath).HasMaxLength(300);
        builder.Property(lead => lead.Referrer).HasMaxLength(2000);
        builder.Property(lead => lead.UserAgent).HasMaxLength(500);

        builder.HasIndex(lead => lead.Email);
        builder.HasIndex(lead => new { lead.Source, lead.CreatedAt });
        builder.HasIndex(lead => new { lead.Status, lead.CreatedAt });
    }
}