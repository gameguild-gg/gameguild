using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for BlockchainCertificateAnchor
/// </summary>
public class BlockchainCertificateAnchorConfiguration : IEntityTypeConfiguration<BlockchainCertificateAnchor>
{
    public void Configure(EntityTypeBuilder<BlockchainCertificateAnchor> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("blockchaincertificateanchor", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CertificateType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CertificateHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CertificateData).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.TransactionHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.BlockchainNetwork).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AnchoredAt).IsRequired();
        builder.Property(x => x.IsRevoked).IsRequired();
        builder.Property(x => x.RevocationReason).HasMaxLength(1000);
        builder.Property(x => x.RevocationTransactionHash).HasMaxLength(128);
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_blockchaincertificateanchor_user_id");
        builder.HasIndex(x => x.CertificateHash).IsUnique().HasDatabaseName("ix_blockchaincertificateanchor_certificate_hash");
        builder.HasIndex(x => x.TransactionHash).HasDatabaseName("ix_blockchaincertificateanchor_transaction_hash");
    }
}
