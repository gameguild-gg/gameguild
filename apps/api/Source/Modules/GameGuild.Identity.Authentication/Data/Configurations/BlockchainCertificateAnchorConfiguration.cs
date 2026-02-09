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
        builder.ToTable("blockchain_certificate_anchors", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CertificateType).HasColumnName("certificate_type").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CertificateHash).HasColumnName("certificate_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CertificateData).HasColumnName("certificate_data").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.TransactionHash).HasColumnName("transaction_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.BlockchainNetwork).HasColumnName("blockchain_network").HasMaxLength(64).IsRequired();
        builder.Property(x => x.BlockNumber).HasColumnName("block_number");
        builder.Property(x => x.AnchoredAt).HasColumnName("anchored_at").IsRequired();
        builder.Property(x => x.IsRevoked).HasColumnName("is_revoked").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(1000);
        builder.Property(x => x.RevocationTransactionHash).HasColumnName("revocation_transaction_hash").HasMaxLength(128);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasMaxLength(2000);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_blockchaincertificateanchor_user_id");
        builder.HasIndex(x => x.CertificateHash).IsUnique().HasDatabaseName("ix_blockchaincertificateanchor_certificate_hash");
        builder.HasIndex(x => x.TransactionHash).HasDatabaseName("ix_blockchaincertificateanchor_transaction_hash");
    }
}
