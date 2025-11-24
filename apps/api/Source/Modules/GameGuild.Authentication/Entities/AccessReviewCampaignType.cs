namespace GameGuild.Authentication.Entities;

/// <summary>
///     Access review campaign type
/// </summary>
public enum AccessReviewCampaignType
{
    /// <summary>
    ///     Ad-hoc review campaign
    /// </summary>
    AdHoc = 1,

    /// <summary>
    ///     Quarterly review campaign
    /// </summary>
    Quarterly = 2,

    /// <summary>
    ///     Annual review campaign
    /// </summary>
    Annual = 3,

    /// <summary>
    ///     Semi-annual review campaign
    /// </summary>
    SemiAnnual = 4,

    /// <summary>
    ///     Monthly review campaign
    /// </summary>
    Monthly = 5
}
