namespace GameGuild.Identity.Authentication;

/// <summary>
///     Statistics for conditional policy enforcement
/// </summary>
public abstract class ConditionalPolicyStatistics
{
    /// <summary>
    ///     Total number of policies
    /// </summary>
    public int TotalPolicies { get; set; }

    /// <summary>
    ///     Number of enabled policies
    /// </summary>
    public int EnabledPolicies { get; set; }

    /// <summary>
    ///     Total policy evaluations
    /// </summary>
    public long TotalEvaluations { get; set; }

    /// <summary>
    ///     Number of times policies denied access
    /// </summary>
    public long DenyActions { get; set; }

    /// <summary>
    ///     Number of times policies required additional auth
    /// </summary>
    public long AdditionalAuthRequests { get; set; }

    /// <summary>
    ///     Average evaluation time in milliseconds
    /// </summary>
    public double AverageEvaluationTimeMs { get; set; }

    /// <summary>
    ///     Statistics collected from date
    /// </summary>
    public DateTime From { get; set; }

    /// <summary>
    ///     Statistics collected to date
    /// </summary>
    public DateTime To { get; set; }
}
