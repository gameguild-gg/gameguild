namespace GameGuild.Identity.Authentication;

/// <summary>
///     ABAC policy statistics
/// </summary>
public abstract class AbacPolicyStatistics
{
    /// <summary>
    ///     Total number of policies
    /// </summary>
    public int TotalPolicies { get; set; }

    /// <summary>
    ///     Number of active policies
    /// </summary>
    public int ActivePolicies { get; set; }

    /// <summary>
    ///     Number of evaluations performed
    /// </summary>
    public long TotalEvaluations { get; set; }

    /// <summary>
    ///     Number of allow decisions
    /// </summary>
    public long AllowDecisions { get; set; }

    /// <summary>
    ///     Number of deny decisions
    /// </summary>
    public long DenyDecisions { get; set; }

    /// <summary>
    ///     Average evaluation time in milliseconds
    /// </summary>
    public double AverageEvaluationTimeMs { get; set; }

    /// <summary>
    ///     Policy hit rate (percentage of evaluations that matched at least one policy)
    /// </summary>
    public double PolicyHitRate { get; set; }

    /// <summary>
    ///     Statistics collected from date
    /// </summary>
    public DateTime From { get; set; }

    /// <summary>
    ///     Statistics collected to date
    /// </summary>
    public DateTime To { get; set; }
}
