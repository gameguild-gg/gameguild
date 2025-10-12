namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Entity representing evidence for a dispute</summary>
[Table("dispute_evidence")]
[Index(nameof(DisputeId))]
[Index(nameof(EvidenceType))]
[Index(nameof(SubmittedAt))]
public class DisputeEvidence : EntityBase
{
    /// <summary>Foreign key to the PaymentDispute entity</summary>
    [Required]
    public Guid DisputeId { get; set; }

    /// <summary>Navigation property to the PaymentDispute entity</summary>
    [ForeignKey(nameof(DisputeId))]
    public virtual PaymentDispute Dispute { get; set; } = null!;

    /// <summary>Evidence type</summary>
    public EvidenceType EvidenceType { get; set; }

    /// <summary>Evidence title</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Evidence description</summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>File URL (if applicable)</summary>
    [MaxLength(500)]
    public string? FileUrl { get; set; }

    /// <summary>File name</summary>
    [MaxLength(255)]
    public string? FileName { get; set; }

    /// <summary>File size in bytes</summary>
    public long? FileSize { get; set; }

    /// <summary>File mime type</summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }

    /// <summary>Submitted timestamp</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Submitted by user ID</summary>
    [Required]
    public Guid SubmittedBy { get; set; }

    /// <summary>Whether this evidence is from merchant</summary>
    public bool IsFromMerchant { get; set; } = false;

    /// <summary>Additional metadata (JSON)</summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }
}

/// <summary>Evidence types</summary>
public enum EvidenceType
{
    /// <summary>Receipt or invoice</summary>
    Receipt = 0,

    /// <summary>Communication (email, chat, etc.)</summary>
    Communication = 1,

    /// <summary>Photo or image</summary>
    Photo = 2,

    /// <summary>Video</summary>
    Video = 3,

    /// <summary>Shipping/tracking information</summary>
    ShippingInfo = 4,

    /// <summary>Contract or agreement</summary>
    Contract = 5,

    /// <summary>Bank statement</summary>
    BankStatement = 6,

    /// <summary>Product/service documentation</summary>
    Documentation = 7,

    /// <summary>Other supporting document</summary>
    Other = 8
}
