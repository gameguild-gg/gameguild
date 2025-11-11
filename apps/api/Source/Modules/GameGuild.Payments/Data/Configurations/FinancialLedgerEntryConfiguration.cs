using GameGuild.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Payments.Data.Configurations;

/// <summary>
///     Entity Type Configuration for FinancialLedgerEntry
/// </summary>
public class FinancialLedgerEntryConfiguration : IEntityTypeConfiguration<FinancialLedgerEntry>
{
    public void Configure(EntityTypeBuilder<FinancialLedgerEntry> builder)
    {
        // Configure table with constraints for EF Core 9.0
        builder.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_FinancialLedgerEntry_DebitAccount_NotEmpty", "LENGTH(debit_account) > 0");
                tb.HasCheckConstraint("CK_FinancialLedgerEntry_CreditAccount_NotEmpty", "LENGTH(credit_account) > 0");
                tb.HasCheckConstraint("CK_FinancialLedgerEntry_Amount_Positive", "amount > 0");
            }
        );

        // Primary key configuration
        builder.HasKey(x => x.Id);

        // Property configurations based on entity attributes
        builder.Property(x => x.EntryType).HasConversion<string>().IsRequired();

        builder.Property(x => x.DebitAccount).HasMaxLength(100).IsRequired();

        builder.Property(x => x.CreditAccount).HasMaxLength(100).IsRequired();

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(x => x.IsReconciled).IsRequired();

        // Configure indexes for performance
        builder.HasIndex(x => x.EntryType);
        builder.HasIndex(x => x.DebitAccount);
        builder.HasIndex(x => x.CreditAccount);
        builder.HasIndex(x => x.ReferenceNumber);
        builder.HasIndex(x => x.IsReconciled);
        builder.HasIndex(x => x.FiscalYear);
        builder.HasIndex(x => x.FiscalPeriod);
        builder.HasIndex(x => x.CreatedAt);
    }
}
