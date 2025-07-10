using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Certificates;

[Table("certificate_blockchain_anchors")]
[Index(nameof(CertificateId))]
[Index(nameof(TransactionHash), IsUnique = true)]
[Index(nameof(BlockchainNetwork))]
[Index(nameof(Status))]
[Index(nameof(AnchoredAt))]
public class CertificateBlockchainAnchor : Entity {
  public Guid CertificateId { get; set; }

  [Required] [MaxLength(100)] public string BlockchainNetwork { get; set; } = string.Empty;

  [Required] [MaxLength(200)] public string TransactionHash { get; set; } = string.Empty;

  [MaxLength(200)] public string? BlockHash { get; set; }

  public long? BlockNumber { get; set; }

  [MaxLength(500)] public string? ContractAddress { get; set; }

  [MaxLength(100)] public string? TokenId { get; set; }

  /// <summary>
  /// Status of the blockchain anchoring
  /// </summary>
  [MaxLength(50)]
  public string Status { get; set; } = "pending";

  /// <summary>
  /// Date when the anchoring was initiated
  /// </summary>
  public DateTime AnchoredAt { get; set; }

  /// <summary>
  /// Date when the anchoring was confirmed on-chain
  /// </summary>
  public DateTime? ConfirmedAt { get; set; }

  // Navigation properties
  [ForeignKey(nameof(CertificateId))] public virtual UserCertificate Certificate { get; set; } = null!;
}
