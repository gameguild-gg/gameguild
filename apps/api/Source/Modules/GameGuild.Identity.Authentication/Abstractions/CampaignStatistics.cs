namespace GameGuild.Identity.Authentication;

/// <summary>
///     Access review campaign statistics
/// </summary>
public abstract class CampaignStatistics
{
    /// <summary>
    ///     Total number of items in the campaign
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Number of items reviewed
    /// </summary>
    public int Reviewed { get; set; }

    /// <summary>
    ///     Number of items pending review
    /// </summary>
    public int Pending { get; set; }

    /// <summary>
    ///     Number of items approved
    /// </summary>
    public int Approved { get; set; }

    /// <summary>
    ///     Number of items revoked
    /// </summary>
    public int Revoked { get; set; }

    /// <summary>
    ///     Completion percentage
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    ///     Average review time in minutes
    /// </summary>
    public double AverageReviewTimeMinutes { get; set; }
}
