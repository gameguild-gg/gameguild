using System.ComponentModel;

namespace GameGuild.Modules.Contents.Models;

/// <summary>
/// Enumeration of content types for categorization
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Article or blog post content
    /// </summary>
    [Description("Article")]
    Article = 0,

    /// <summary>
    /// Video content
    /// </summary>
    [Description("Video")]
    Video = 1,

    /// <summary>
    /// Audio or podcast content
    /// </summary>
    [Description("Audio")]
    Audio = 2,

    /// <summary>
    /// Image or gallery content
    /// </summary>
    [Description("Image")]
    Image = 3,

    /// <summary>
    /// Document or file content
    /// </summary>
    [Description("Document")]
    Document = 4,

    /// <summary>
    /// Course or learning material
    /// </summary>
    [Description("Course")]
    Course = 5,

    /// <summary>
    /// Project or portfolio item
    /// </summary>
    [Description("Project")]
    Project = 6,

    /// <summary>
    /// News or announcement
    /// </summary>
    [Description("News")]
    News = 7,

    /// <summary>
    /// Tutorial or how-to guide
    /// </summary>
    [Description("Tutorial")]
    Tutorial = 8,

    /// <summary>
    /// Event or webinar
    /// </summary>
    [Description("Event")]
    Event = 9,

    /// <summary>
    /// FAQ or knowledge base entry
    /// </summary>
    [Description("FAQ")]
    FAQ = 10,

    /// <summary>
    /// Page or static content
    /// </summary>
    [Description("Page")]
    Page = 11,
}
