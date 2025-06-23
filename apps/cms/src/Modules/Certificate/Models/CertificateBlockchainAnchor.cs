using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Certificate.Models;

[Table("certificate_blockchain_anchors")]
[Index(nameof(CertificateId))]
[Index(nameof(TransactionHash), IsUnique = true)]
[Index(nameof(BlockchainNetwork))]
[Index(nameof(Status))]
[Index(nameof(AnchoredAt))]
public class CertificateBlockchainAnchor : BaseEntity
{
    private Guid _certificateId;

    private string _blockchainNetwork = string.Empty;

    private string _transactionHash = string.Empty;

    private string? _blockHash;

    private long? _blockNumber;

    private string? _contractAddress;

    private string? _tokenId;

    private string _status = "pending";

    private DateTime _anchoredAt;

    private DateTime? _confirmedAt;

    private UserCertificate _certificate = null!;

    public Guid CertificateId
    {
        get => _certificateId;
        set => _certificateId = value;
    }

    [Required]
    [MaxLength(100)]
    public string BlockchainNetwork
    {
        get => _blockchainNetwork;
        set => _blockchainNetwork = value;
    }

    [Required]
    [MaxLength(200)]
    public string TransactionHash
    {
        get => _transactionHash;
        set => _transactionHash = value;
    }

    [MaxLength(200)]
    public string? BlockHash
    {
        get => _blockHash;
        set => _blockHash = value;
    }

    public long? BlockNumber
    {
        get => _blockNumber;
        set => _blockNumber = value;
    }

    [MaxLength(500)]
    public string? ContractAddress
    {
        get => _contractAddress;
        set => _contractAddress = value;
    }

    [MaxLength(100)]
    public string? TokenId
    {
        get => _tokenId;
        set => _tokenId = value;
    }

    /// <summary>
    /// Status of the blockchain anchoring
    /// </summary>
    [MaxLength(50)]
    public string Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// Date when the anchoring was initiated
    /// </summary>
    public DateTime AnchoredAt
    {
        get => _anchoredAt;
        set => _anchoredAt = value;
    }

    /// <summary>
    /// Date when the anchoring was confirmed on-chain
    /// </summary>
    public DateTime? ConfirmedAt
    {
        get => _confirmedAt;
        set => _confirmedAt = value;
    }

    // Navigation properties
    [ForeignKey(nameof(CertificateId))]
    public virtual UserCertificate Certificate
    {
        get => _certificate;
        set => _certificate = value;
    }
}

public class CertificateBlockchainAnchorConfiguration : IEntityTypeConfiguration<CertificateBlockchainAnchor>
{
    public void Configure(EntityTypeBuilder<CertificateBlockchainAnchor> builder)
    {
        // Configure relationship with Certificate (can't be done with annotations)
        builder.HasOne(cba => cba.Certificate)
            .WithMany(uc => uc.BlockchainAnchors)
            .HasForeignKey(cba => cba.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
