using System.ComponentModel.DataAnnotations;

namespace GameGuild.Common.Entities;

/// <summary>
/// Stores statistics and metadata for content.
/// </summary>
public class ContentMetadata
{
    private Guid _id;

    private Content _content = null!;

    private Guid _contentId;

    private int _viewCount = 0;

    private int _downloadCount = 0;

    private int _likeCount = 0;

    private int _shareCount = 0;

    private int _commentCount = 0;

    private string? _metaDescription;

    private string? _metaKeywords;

    private DateTime? _lastViewedAt;

    private DateTime? _lastInteractionAt;

    [Key]
    public Guid Id
    {
        get => _id;
        set => _id = value;
    }

    /// <summary>
    /// Navigation property to the content
    /// </summary>
    [Required]
    public virtual Content Content
    {
        get => _content;
        set => _content = value;
    }

    public Guid ContentId
    {
        get => _contentId;
        set => _contentId = value;
    }

    /// <summary>
    /// Content statistics
    /// </summary>
    public int ViewCount
    {
        get => _viewCount;
        set => _viewCount = value;
    }

    public int DownloadCount
    {
        get => _downloadCount;
        set => _downloadCount = value;
    }

    public int LikeCount
    {
        get => _likeCount;
        set => _likeCount = value;
    }

    public int ShareCount
    {
        get => _shareCount;
        set => _shareCount = value;
    }

    public int CommentCount
    {
        get => _commentCount;
        set => _commentCount = value;
    }

    /// <summary>
    /// SEO metadata
    /// </summary>
    [MaxLength(160)]
    public string? MetaDescription
    {
        get => _metaDescription;
        set => _metaDescription = value;
    }

    [MaxLength(255)]
    public string? MetaKeywords
    {
        get => _metaKeywords;
        set => _metaKeywords = value;
    }

    /// <summary>
    /// Timestamps for analytics
    /// </summary>
    public DateTime? LastViewedAt
    {
        get => _lastViewedAt;
        set => _lastViewedAt = value;
    }

    public DateTime? LastInteractionAt
    {
        get => _lastInteractionAt;
        set => _lastInteractionAt = value;
    }
}
