using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// Cached transformed version of an asset.
/// Generated on-demand and cached for reuse.
/// </summary>
[Table("transformed_assets")]
[Index(nameof(SourceContentId))]
[Index(nameof(SourceContentId), nameof(TransformationSpec), IsUnique = true)]
[Index(nameof(LastAccessedAt))]
public class TransformedAsset : EntityBase
{
    /// <summary>
    /// Source content ID.
    /// </summary>
    public Guid SourceContentId { get; set; }

    /// <summary>
    /// Canonical transformation spec (normalized, sorted params).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TransformationSpec { get; init; } = string.Empty;

    /// <summary>
    /// S3 bucket name where the transformed content is stored.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BucketName { get; init; } = string.Empty;

    /// <summary>
    /// S3 object key for transformed content.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ObjectKey { get; init; } = string.Empty;

    /// <summary>
    /// MIME type of the transformed content.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Transformed size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Transformed width.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Transformed height.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Last accessed (for cache eviction).
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation

    [ForeignKey(nameof(SourceContentId))]
    public virtual AssetContent SourceContent { get; set; } = null!;

    /// <summary>
    /// Records an access to this transformed asset.
    /// </summary>
    public void RecordAccess() => LastAccessedAt = DateTime.UtcNow;

    /// <summary>
    /// Returns true if this transformed asset should be evicted from cache.
    /// </summary>
    public bool ShouldEvict(TimeSpan maxAge) => DateTime.UtcNow - LastAccessedAt > maxAge;
}
