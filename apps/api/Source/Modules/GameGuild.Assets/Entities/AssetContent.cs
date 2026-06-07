using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// Represents the immutable binary content stored in S3.
/// Multiple AssetReferences can point to the same AssetContent (deduplication).
/// </summary>
[Table("asset_contents")]
[Index(nameof(ContentHash), IsUnique = true)]
[Index(nameof(ModerationStatus))]
[Index(nameof(VirusScanStatus))]
public class AssetContent : EntityBase
{
    /// <summary>
    /// Default constructor for EF Core.
    /// </summary>
    protected AssetContent() { }

    /// <summary>
    /// Creates a new asset content record.
    /// </summary>
    public AssetContent(
        string bucketName,
        string objectKey,
        string contentHash,
        string mimeType,
        long sizeBytes,
        int? width,
        int? height)
    {
        BucketName = bucketName;
        ObjectKey = objectKey;
        ContentHash = contentHash;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Width = width;
        Height = height;
        Kind = DetermineKindFromMimeType(mimeType);
    }

    private static AssetKind DetermineKindFromMimeType(string mimeType)
    {
        if (mimeType.StartsWith("image/")) return AssetKind.Image;
        if (mimeType.StartsWith("video/")) return AssetKind.Video;
        if (mimeType.StartsWith("audio/")) return AssetKind.Audio;
        if (mimeType.StartsWith("application/pdf") || mimeType.Contains("document")) return AssetKind.Document;
        if (mimeType.Contains("zip") || mimeType.Contains("rar") || mimeType.Contains("7z")) return AssetKind.Archive;
        return AssetKind.Other;
    }

    /// <summary>
    /// SHA-256 hash of the content (primary deduplication key).
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>
    /// Perceptual hash for image/video similarity detection.
    /// </summary>
    [MaxLength(64)]
    public string? PerceptualHash { get; set; }

    /// <summary>
    /// S3 bucket name where the content is stored.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BucketName { get; init; } = string.Empty;

    /// <summary>
    /// S3 object key (path within bucket).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ObjectKey { get; init; } = string.Empty;

    /// <summary>
    /// MIME type of the content.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Image/video width in pixels (null for documents/audio).
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Image/video height in pixels (null for documents/audio).
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Video/audio duration in seconds (null for images/documents).
    /// </summary>
    public double? DurationSeconds { get; init; }

    /// <summary>
    /// Content kind classification.
    /// </summary>
    public AssetKind Kind { get; init; }

    /// <summary>
    /// Virus scan status.
    /// </summary>
    public VirusScanStatus VirusScanStatus { get; set; } = VirusScanStatus.Pending;

    /// <summary>
    /// When virus scan completed.
    /// </summary>
    public DateTime? VirusScanCompletedAt { get; set; }

    /// <summary>
    /// Moderation status.
    /// </summary>
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;

    /// <summary>
    /// When moderation completed.
    /// </summary>
    public DateTime? ModerationCompletedAt { get; set; }

    /// <summary>
    /// User who performed the last manual moderation review.
    /// </summary>
    public Guid? ModerationReviewedBy { get; set; }

    /// <summary>
    /// When the last manual moderation review occurred.
    /// </summary>
    public DateTime? ModerationReviewedAt { get; set; }

    /// <summary>
    /// Notes captured during the last manual moderation review.
    /// </summary>
    [MaxLength(2000)]
    public string? ModerationReviewNotes { get; set; }

    /// <summary>
    /// Auto-moderation labels detected (JSON array).
    /// </summary>
    [MaxLength(2000)]
    public string? ModerationLabels { get; set; }

    /// <summary>
    /// Whether this content can ever be deleted.
    /// </summary>
    public bool IsDeletable { get; set; } = true;

    /// <summary>
    /// Reference count for garbage collection eligibility.
    /// </summary>
    public int ReferenceCount { get; set; } = 0;

    /// <summary>
    /// When this became eligible for GC (null if still referenced).
    /// </summary>
    public DateTime? MarkedForDeletionAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual ICollection<AssetReference> References { get; init; } = [];
    public virtual ICollection<TransformedAsset> TransformedVersions { get; init; } = [];

    /// <summary>
    /// Gets moderation labels as a list.
    /// </summary>
    [NotMapped]
    public IReadOnlyList<string> ModerationLabelsList
    {
        get
        {
            if (string.IsNullOrEmpty(ModerationLabels))
                return [];
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ModerationLabels) ?? [];
        }
    }

    /// <summary>
    /// Sets moderation labels from a list.
    /// </summary>
    public void SetModerationLabels(IEnumerable<string> labels)
    {
        ModerationLabels = System.Text.Json.JsonSerializer.Serialize(labels.ToList());
    }

    /// <summary>
    /// Returns true if the content is safe to serve.
    /// </summary>
    [NotMapped]
    public bool IsSafeToServe =>
        VirusScanStatus == VirusScanStatus.Clean &&
        (ModerationStatus == ModerationStatus.Approved || ModerationStatus == ModerationStatus.ApprovedWithWarning);

    /// <summary>
    /// Returns true if the content is pending any processing.
    /// </summary>
    [NotMapped]
    public bool IsPendingProcessing =>
        VirusScanStatus == VirusScanStatus.Pending ||
        VirusScanStatus == VirusScanStatus.Scanning ||
        ModerationStatus == ModerationStatus.Pending ||
        ModerationStatus == ModerationStatus.Processing;

    /// <summary>
    /// Sets the moderation status.
    /// </summary>
    public void SetModerationStatus(ModerationStatus status, IEnumerable<string>? labels = null)
    {
        ModerationStatus = status;
        ModerationCompletedAt = SystemClock.UtcNow;
        if (labels != null)
        {
            SetModerationLabels(labels);
        }
    }

    /// <summary>
    /// Sets the moderation status with full admin review details.
    /// </summary>
    public void SetModerationStatus(ModerationStatus status, Guid reviewedBy, string[]? labels = null, string? notes = null)
    {
        ModerationStatus = status;
        ModerationCompletedAt = SystemClock.UtcNow;
        ModerationReviewedBy = reviewedBy;
        ModerationReviewedAt = ModerationCompletedAt;
        ModerationReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (labels != null)
        {
            SetModerationLabels(labels);
        }
    }

    /// <summary>
    /// Sets the virus scan status.
    /// </summary>
    public void SetVirusScanStatus(VirusScanStatus status, string? scanResult = null)
    {
        VirusScanStatus = status;
        VirusScanCompletedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Marks this content as non-deletable (legal hold).
    /// </summary>
    /// <param name="reason">Optional reason for the hold.</param>
    public void MarkAsNonDeletable(string? reason = null)
    {
        IsDeletable = false;
        MarkedForDeletionAt = null; // Clear any pending deletion
        // Note: reason could be stored if we add a property
    }

    /// <summary>
    /// Marks this content as deletable again.
    /// </summary>
    public void MarkAsDeletable()
    {
        IsDeletable = true;
        // If still has no references, it will be picked up by next GC run
        if (ReferenceCount == 0)
        {
            MarkedForDeletionAt = SystemClock.UtcNow;
        }
    }
}
