namespace GameGuild.Authentication.Entities;

/// <summary>
///     Access review campaign status
/// </summary>
public enum AccessReviewCampaignStatus
{
    /// <summary>
    ///     Campaign is in draft state
    /// </summary>
    Draft = 1,

    /// <summary>
    ///     Campaign is in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    ///     Campaign is completed
    /// </summary>
    Completed = 3,

    /// <summary>
    ///     Campaign was cancelled
    /// </summary>
    Cancelled = 4
}
