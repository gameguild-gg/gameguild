using GameGuild.Localization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// Represents a logical reference to asset content.
/// This is what users interact with - the same content can have multiple references.
/// </summary>
[Table("asset_references")]
[Index(nameof(AssetContentId))]
[Index(nameof(CreatedByUserId))]
[Index(nameof(ParentResourceType), nameof(ParentResourceId))]
[Index(nameof(AccessPolicy))]
public class AssetReference : EntityBase, ILocalizable
{
    /// <summary>
    /// Default constructor for EF Core.
    /// </summary>
    protected AssetReference() { }

    /// <summary>
    /// Creates a new asset reference.
    /// </summary>
    public AssetReference(
        Guid assetContentId,
        Guid createdByUserId,
        string? displayName,
        AssetAccessPolicy accessPolicy,
        string? parentResourceType,
        Guid? parentResourceId)
    {
        AssetContentId = assetContentId;
        CreatedByUserId = createdByUserId;
        DisplayName = displayName;
        AccessPolicy = accessPolicy;
        ParentResourceType = parentResourceType;
        ParentResourceId = parentResourceId;
    }

    /// <summary>
    /// Foreign key to the actual content.
    /// </summary>
    public Guid AssetContentId { get; set; }

    /// <summary>
    /// User who created this reference.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Human-readable name/title.
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Original filename from upload.
    /// </summary>
    [MaxLength(255)]
    public string? OriginalFilename { get; set; }

    /// <summary>
    /// Description (localizable).
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Alt text for accessibility (localizable).
    /// </summary>
    [MaxLength(500)]
    public string? AltText { get; set; }

    /// <summary>
    /// Access policy for this reference.
    /// </summary>
    public AssetAccessPolicy AccessPolicy { get; set; } = AssetAccessPolicy.Private;

    /// <summary>
    /// Parent resource type this asset is attached to.
    /// </summary>
    [MaxLength(100)]
    public string? ParentResourceType { get; set; }

    /// <summary>
    /// Parent resource ID.
    /// </summary>
    public Guid? ParentResourceId { get; set; }

    /// <summary>
    /// Tags for categorization (JSON array).
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Access counter for rate limiting.
    /// </summary>
    public long AccessCount { get; set; } = 0;

    /// <summary>
    /// Last access time.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Download window expiry for paid content.
    /// </summary>
    public DateTime? DownloadWindowExpiresAt { get; set; }

    /// <summary>
    /// Order ID that granted download access.
    /// </summary>
    public Guid? GrantedByOrderId { get; set; }

    // Navigation properties

    [ForeignKey(nameof(AssetContentId))]
    public virtual AssetContent Content { get; set; } = null!;

    public virtual ICollection<AssetReport> Reports { get; init; } = [];

    public virtual ICollection<ResourceLocalization> Localizations { get; set; } = [];

    #region ILocalizable Implementation

    public ResourceLocalization AddLocalization(
        string fieldName,
        string content,
        Language language,
        LocalizationStatus status = LocalizationStatus.Draft)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(language);

        var localization = new ResourceLocalization
        {
            Id = Guid.NewGuid(),
            ResourceId = Id,
            ResourceType = nameof(AssetReference),
            FieldName = fieldName,
            Content = content,
            LanguageId = language.Id,
            Language = language,
            Status = status
        };

        Localizations.Add(localization);
        return localization;
    }

    #endregion

    /// <summary>
    /// Gets tags as a list.
    /// </summary>
    [NotMapped]
    public IReadOnlyList<string> TagsList
    {
        get
        {
            if (string.IsNullOrEmpty(Tags)) return [];
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags) ?? [];
        }
    }

    /// <summary>
    /// Sets tags from a list.
    /// </summary>
    public void SetTags(IEnumerable<string> tags) =>
        Tags = System.Text.Json.JsonSerializer.Serialize(tags.ToList());

    /// <summary>
    /// Records an access to this asset.
    /// </summary>
    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the download window is valid.
    /// </summary>
    [NotMapped]
    public bool IsDownloadWindowValid =>
        !DownloadWindowExpiresAt.HasValue || DownloadWindowExpiresAt.Value > DateTime.UtcNow;

    /// <summary>
    /// Updates the display name.
    /// </summary>
    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Updates the access policy.
    /// </summary>
    public void UpdateAccessPolicy(AssetAccessPolicy accessPolicy)
    {
        AccessPolicy = accessPolicy;
    }
}
