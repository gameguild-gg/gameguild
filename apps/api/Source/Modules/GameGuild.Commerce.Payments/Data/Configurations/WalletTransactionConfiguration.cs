using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for WalletTransaction
/// </summary>
public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        // Configure table name with constraints
        builder.ToTable("wallet_transactions", tb =>
        {
            tb.HasCheckConstraint("CK_WalletTransaction_Amount_Positive", "amount > 0");
        });

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.WalletId)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasMaxLength(2000);

        // Relationship configurations
        builder.HasOne(x => x.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(x => x.WalletId).HasDatabaseName("ix_wallet_transactions_wallet_id");
        builder.HasIndex(x => x.Type).HasDatabaseName("ix_wallet_transactions_type");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_wallet_transactions_status");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_wallet_transactions_created_at");
        builder.HasIndex(x => x.ReferenceId).HasDatabaseName("ix_wallet_transactions_reference_id");
    }
}
