using System.ComponentModel;

namespace GameGuild;

/// <summary>
/// Enumeration of content status values for lifecycle management
/// </summary>
public enum ContentStatus {
    /// <summary>
    /// Content is in draft state and not visible
    /// </summary>
    [Description("Draft")]
    Draft = 0,

    /// <summary>
    /// Content is pending review
    /// </summary>
    [Description("Pending Review")]
    PendingReview = 1,

    /// <summary>
    /// Content is under review
    /// </summary>
    [Description("In Review")]
    InReview = 2,

    /// <summary>
    /// Content is approved and ready to publish
    /// </summary>
    [Description("Approved")]
    Approved = 3,

    /// <summary>
    /// Content is published and visible
    /// </summary>
    [Description("Published")]
    Published = 4,

    /// <summary>
    /// Content is scheduled for future publication
    /// </summary>
    [Description("Scheduled")]
    Scheduled = 5,

    /// <summary>
    /// Content is archived and no longer active
    /// </summary>
    [Description("Archived")]
    Archived = 6,

    /// <summary>
    /// Content has been rejected
    /// </summary>
    [Description("Rejected")]
    Rejected = 7,

    /// <summary>
    /// Content has been deleted
    /// </summary>
    [Description("Deleted")]
    Deleted = 8,

    /// <summary>
    /// Content is under review (alias for InReview)
    /// </summary>
    [Description("Under Review")]
    UnderReview = 2
}
