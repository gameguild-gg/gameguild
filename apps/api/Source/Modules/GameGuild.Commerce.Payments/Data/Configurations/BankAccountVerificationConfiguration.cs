using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for <see cref="BankAccountVerification"/> (issue #414).
/// </summary>
public class BankAccountVerificationConfiguration : IEntityTypeConfiguration<BankAccountVerification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BankAccountVerification> builder)
    {
        builder.ToTable("bank_account_verifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Deposit1Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Deposit2Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Salt)
            .IsRequired()
            .HasMaxLength(64);

        // Latest-first lookup of a method's current challenge.
        builder.HasIndex(x => new { x.PaymentMethodId, x.InitiatedAtUtc })
            .IsUnique()
            .HasDatabaseName("ix_bank_account_verifications_method_initiated");

        builder.HasIndex(x => x.PaymentMethodId)
            .HasDatabaseName("ix_bank_account_verifications_method");
    }
}
