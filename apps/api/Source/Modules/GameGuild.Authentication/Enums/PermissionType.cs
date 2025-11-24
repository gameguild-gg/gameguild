namespace GameGuild.Authentication.Enums;

/// <summary>
///     Comprehensive enumeration of permission types in the GameGuild system
///     Represents the various operations that can be controlled through permissions
///     Based on Game Guild's 251-value permission system with full feature parity
/// </summary>
public enum PermissionType
{
    #region Interaction GameGuild.Permissions

    /// <summary>
    ///     Permission to read/view content
    /// </summary>
    Read = 1,

    /// <summary>
    ///     Permission to comment on content
    /// </summary>
    Comment = 2,

    /// <summary>
    ///     Permission to reply to comments
    /// </summary>
    Reply = 3,

    /// <summary>
    ///     Permission to vote on content
    /// </summary>
    Vote = 4,

    /// <summary>
    ///     Permission to share content
    /// </summary>
    Share = 5,

    /// <summary>
    ///     Permission to report content
    /// </summary>
    Report = 6,

    /// <summary>
    ///     Permission to follow users or content
    /// </summary>
    Follow = 7,

    /// <summary>
    ///     Permission to bookmark content
    /// </summary>
    Bookmark = 8,

    /// <summary>
    ///     Permission to react to content
    /// </summary>
    React = 9,

    /// <summary>
    ///     Permission to subscribe to content or notifications
    /// </summary>
    Subscribe = 10,

    /// <summary>
    ///     Permission to mention users
    /// </summary>
    Mention = 11,

    /// <summary>
    ///     Permission to tag content
    /// </summary>
    Tag = 12,

    #endregion

    #region Curation GameGuild.Permissions

    /// <summary>
    ///     Permission to categorize content
    /// </summary>
    Categorize = 13,

    /// <summary>
    ///     Permission to create and manage collections
    /// </summary>
    Collection = 14,

    /// <summary>
    ///     Permission to create and manage series
    /// </summary>
    Series = 15,

    /// <summary>
    ///     Permission to create cross-references between content
    /// </summary>
    CrossReference = 16,

    /// <summary>
    ///     Permission to translate content
    /// </summary>
    Translate = 17,

    /// <summary>
    ///     Permission to create versions of content
    /// </summary>
    Version = 18,

    /// <summary>
    ///     Permission to create and use templates
    /// </summary>
    Template = 19,

    #endregion

    #region Lifecycle GameGuild.Permissions

    /// <summary>
    ///     Permission to create new content
    /// </summary>
    Create = 20,

    /// <summary>
    ///     Permission to save content as draft
    /// </summary>
    Draft = 21,

    /// <summary>
    ///     Permission to submit content for review/publication
    /// </summary>
    Submit = 22,

    /// <summary>
    ///     Permission to withdraw submitted content
    /// </summary>
    Withdraw = 23,

    /// <summary>
    ///     Permission to archive content
    /// </summary>
    Archive = 24,

    /// <summary>
    ///     Permission to restore archived content
    /// </summary>
    Restore = 25,

    /// <summary>
    ///     Permission to soft delete content (Delete is an alias for SoftDelete)
    /// </summary>
    Delete = 26,

    /// <summary>
    ///     Permission to soft delete content (same as Delete)
    ///     Only the owners of a resource can soft delete it at resource level,
    ///     it still can be deleted by admins at tenant or content type level
    /// </summary>
    SoftDelete = 26,

    /// <summary>
    ///     Permission to permanently delete content
    /// </summary>
    HardDelete = 27,

    /// <summary>
    ///     Permission to backup content
    /// </summary>
    Backup = 28,

    /// <summary>
    ///     Permission to migrate content
    /// </summary>
    Migrate = 29,

    /// <summary>
    ///     Permission to clone/duplicate content
    /// </summary>
    Clone = 30,

    #endregion

    #region Editorial GameGuild.Permissions

    /// <summary>
    ///     Permission to edit content
    /// </summary>
    Edit = 31,

    /// <summary>
    ///     Permission to proofread content
    /// </summary>
    Proofread = 32,

    /// <summary>
    ///     Permission to fact-check content
    /// </summary>
    FactCheck = 33,

    /// <summary>
    ///     Permission to apply style guide rules
    /// </summary>
    StyleGuide = 34,

    /// <summary>
    ///     Permission to check for plagiarism
    /// </summary>
    Plagiarism = 35,

    /// <summary>
    ///     Permission to optimize for SEO
    /// </summary>
    Seo = 36,

    /// <summary>
    ///     Permission to ensure accessibility compliance
    /// </summary>
    Accessibility = 37,

    /// <summary>
    ///     Permission to review for legal compliance
    /// </summary>
    Legal = 38,

    /// <summary>
    ///     Permission to ensure brand compliance
    /// </summary>
    Brand = 39,

    /// <summary>
    ///     Permission to enforce content guidelines
    /// </summary>
    Guidelines = 40,

    #endregion

    #region Approval GameGuild.Permissions

    /// <summary>
    ///     Permission to approve content for publication
    /// </summary>
    Approve = 41,

    /// <summary>
    ///     Permission to reject submitted content
    /// </summary>
    Reject = 42,

    /// <summary>
    ///     Permission to request revisions to content
    /// </summary>
    RequestRevision = 43,

    /// <summary>
    ///     Permission to escalate approval decisions
    /// </summary>
    Escalate = 44,

    /// <summary>
    ///     Permission to override approval decisions
    /// </summary>
    Override = 45,

    /// <summary>
    ///     Permission to delegate approval authority
    /// </summary>
    Delegate = 46,

    /// <summary>
    ///     Permission to fast-track approval process
    /// </summary>
    FastTrack = 47,

    /// <summary>
    ///     Permission to batch approve multiple items
    /// </summary>
    BatchApprove = 48,

    /// <summary>
    ///     Permission to conditionally approve content
    /// </summary>
    ConditionalApprove = 49,

    /// <summary>
    ///     Permission to require additional review
    /// </summary>
    RequireReview = 50,

    #endregion

    #region Publishing GameGuild.Permissions

    /// <summary>
    ///     Permission to publish content
    /// </summary>
    Publish = 51,

    /// <summary>
    ///     Permission to unpublish content
    /// </summary>
    Unpublish = 52,

    /// <summary>
    ///     Permission to schedule publication
    /// </summary>
    Schedule = 53,

    /// <summary>
    ///     Permission to set publication dates
    /// </summary>
    SetPublishDate = 54,

    /// <summary>
    ///     Permission to control content visibility
    /// </summary>
    Visibility = 55,

    /// <summary>
    ///     Permission to feature content prominently
    /// </summary>
    Feature = 56,

    /// <summary>
    ///     Permission to pin content to top
    /// </summary>
    Pin = 57,

    /// <summary>
    ///     Permission to sticky content (keep at top)
    /// </summary>
    Sticky = 58,

    /// <summary>
    ///     Permission to highlight content
    /// </summary>
    Highlight = 59,

    /// <summary>
    ///     Permission to promote content
    /// </summary>
    Promote = 60,

    #endregion

    #region Moderation GameGuild.Permissions

    /// <summary>
    ///     Permission to moderate content
    /// </summary>
    Moderate = 61,

    /// <summary>
    ///     Permission to hide content from public view
    /// </summary>
    Hide = 62,

    /// <summary>
    ///     Permission to flag content for review
    /// </summary>
    Flag = 63,

    /// <summary>
    ///     Permission to warn users about content issues
    /// </summary>
    Warn = 64,

    /// <summary>
    ///     Permission to suspend content temporarily
    /// </summary>
    Suspend = 65,

    /// <summary>
    ///     Permission to ban content permanently
    /// </summary>
    Ban = 66,

    /// <summary>
    ///     Permission to quarantine suspicious content
    /// </summary>
    Quarantine = 67,

    /// <summary>
    ///     Permission to review flagged content
    /// </summary>
    Review = 68,

    /// <summary>
    ///     Permission to investigate content issues
    /// </summary>
    Investigate = 69,

    /// <summary>
    ///     Permission to escalate moderation issues
    /// </summary>
    EscalateModeration = 70,

    #endregion

    #region Collaboration GameGuild.Permissions

    /// <summary>
    ///     Permission to invite collaborators
    /// </summary>
    Invite = 71,

    /// <summary>
    ///     Permission to assign tasks to team members
    /// </summary>
    Assign = 72,

    /// <summary>
    ///     Permission to collaborate on content
    /// </summary>
    Collaborate = 73,

    /// <summary>
    ///     Permission to co-author content
    /// </summary>
    CoAuthor = 74,

    /// <summary>
    ///     Permission to contribute to content
    /// </summary>
    Contribute = 75,

    /// <summary>
    ///     Permission to provide suggestions
    /// </summary>
    Suggest = 76,

    /// <summary>
    ///     Permission to track changes in content
    /// </summary>
    Track = 77,

    /// <summary>
    ///     Permission to merge changes from contributors
    /// </summary>
    Merge = 78,

    /// <summary>
    ///     Permission to resolve conflicts in collaborative editing
    /// </summary>
    Resolve = 79,

    /// <summary>
    ///     Permission to coordinate team activities
    /// </summary>
    Coordinate = 80,

    #endregion

    #region Quality Control GameGuild.Permissions

    /// <summary>
    ///     Permission to score content quality
    /// </summary>
    Score = 82,

    /// <summary>
    ///     Permission to rate content
    /// </summary>
    Rate = 83,

    /// <summary>
    ///     Permission to benchmark content against standards
    /// </summary>
    Benchmark = 84,

    /// <summary>
    ///     Permission to collect metrics on content
    /// </summary>
    Metrics = 85,

    /// <summary>
    ///     Permission to access analytics data
    /// </summary>
    Analytics = 86,

    /// <summary>
    ///     Permission to monitor performance
    /// </summary>
    Performance = 87,

    /// <summary>
    ///     Permission to provide feedback
    /// </summary>
    Feedback = 88,

    /// <summary>
    ///     Permission to audit content and processes
    /// </summary>
    Audit = 89,

    /// <summary>
    ///     Permission to enforce quality standards
    /// </summary>
    Standards = 90,

    /// <summary>
    ///     Permission to implement improvements
    /// </summary>
    Improvement = 91,

    #endregion

    #region Administrative GameGuild.Permissions

    /// <summary>
    ///     Permission to manage resources and settings
    /// </summary>
    Manage = 100,

    /// <summary>
    ///     Administrative permission for general admin tasks
    /// </summary>
    Admin = 101,

    /// <summary>
    ///     Permission to execute system operations
    /// </summary>
    Execute = 110,

    /// <summary>
    ///     Permission to export data
    /// </summary>
    Export = 111,

    /// <summary>
    ///     Permission to import data
    /// </summary>
    Import = 112,

    /// <summary>
    ///     System administrator with full system access
    /// </summary>
    SystemAdmin = 200,

    /// <summary>
    ///     Tenant administrator with full tenant access
    /// </summary>
    TenantAdmin = 201,

    /// <summary>
    ///     Permission to manage users and user accounts
    /// </summary>
    UserManagement = 202,

    /// <summary>
    ///     Permission to configure system settings and integrations
    /// </summary>
    Configure = 203,

    #endregion
}
