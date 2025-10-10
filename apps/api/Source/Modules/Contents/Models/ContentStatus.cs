using System.ComponentModel;

namespace GameGuild.Source.Modules.Contents.Models;

/// <summary>
/// Enumeration of content status values for lifecycle management
/// </summary>
public enum ContentStatus
{
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
    /// Content is rejected and needs revision
    /// </summary>
    [Description("Rejected")]
    Rejected = 7,

    /// <summary>
    /// Content is unpublished but can be republished
    /// </summary>
    [Description("Unpublished")]
    Unpublished = 8,
}
