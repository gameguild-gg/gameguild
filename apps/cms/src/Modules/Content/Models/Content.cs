using System.ComponentModel.DataAnnotations;

namespace GameGuild.Common.Entities;

/// <summary>
/// Represents a content item that is a specialized resource.
/// </summary>
public abstract class Content : ResourceBase
{
    private ICollection<ContentLicense> _licenses = new List<ContentLicense>();

    private string _slug = string.Empty;

    private ContentStatus _status = ContentStatus.Draft;
    // Content-specific properties can be added here if needed

    /// <summary>
    /// Licenses associated with this content (many-to-many)
    /// </summary>
    public virtual ICollection<ContentLicense> Licenses
    {
        get => _licenses;
        set => _licenses = value;
    }

    /// <summary>
    /// Slug for the content (URL-friendly unique identifier)
    /// </summary>
    [MaxLength(255)]
    public string Slug
    {
        get => _slug;
        set => _slug = value;
    }

    /// <summary>
    /// Status of the content (draft, published, etc.)
    /// </summary>
    public ContentStatus Status
    {
        get => _status;
        set => _status = value;
    }
}
