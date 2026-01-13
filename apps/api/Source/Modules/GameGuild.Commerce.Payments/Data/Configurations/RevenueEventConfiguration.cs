using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for RevenueEvent
/// </summary>
public class RevenueEventConfiguration : IEntityTypeConfiguration<RevenueEvent>
{
    public void Configure(EntityTypeBuilder<RevenueEvent> builder)
    {
        // Configure table with constraints
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_RevenueEvent_Amount_Positive", "amount > 0");
            tb.HasCheckConstraint("CK_RevenueEvent_ReferenceId_NotEmpty", "LENGTH(reference_id) > 0");
        });

        // Primary key configuration
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ProcessingNotes)
            .HasMaxLength(1000);

        // Configure indexes for performance
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ReferenceId);

        // Note: Relationship to FinancialLedgerEntry is configured in FinancialLedgerEntryConfiguration
        // to avoid circular configuration issues
    }
}
