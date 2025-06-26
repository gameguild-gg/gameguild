using System.ComponentModel.DataAnnotations;

namespace GameGuild.Common.Entities;

/// <summary>
/// Represents a license that can be assigned to content.
/// </summary>
public class ContentLicense : ResourceBase
{
    private string? _url;

    private ICollection<Content> _contents = new List<Content>();

    /// <summary>
    /// Optional URL to the license text
    /// </summary>
    [MaxLength(500)]
    public string? Url
    {
        get => _url;
        set => _url = value;
    }

    /// <summary>
    /// Navigation property to content items using this license
    /// </summary>
    public virtual ICollection<Content> Contents
    {
        get => _contents;
        set => _contents = value;
    }
}
