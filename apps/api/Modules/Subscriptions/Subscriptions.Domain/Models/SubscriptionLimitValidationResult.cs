namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
///     Result of subscription limit validation
/// </summary>
public class SubscriptionLimitValidationResult
{
    /// <summary>
    ///     Whether the current usage is within limits
    /// </summary>
    public bool IsWithinLimits { get; init; }

    /// <summary>
    ///     Details about each limit check
    /// </summary>
    public List<LimitCheckResult> LimitChecks { get; init; } = new List<LimitCheckResult>();

    /// <summary>
    ///     Overall validation message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Recommended action if limits are exceeded
    /// </summary>
    public string? RecommendedAction { get; init; }

    /// <summary>
    ///     Creates a valid result
    /// </summary>
    public static SubscriptionLimitValidationResult Valid(string? message = null)
    {
        return new SubscriptionLimitValidationResult
        {
            IsWithinLimits = true, Message = message ?? "All limits are within allowed ranges"
        };
    }

    /// <summary>
    ///     Creates an invalid result
    /// </summary>
    public static SubscriptionLimitValidationResult Invalid(List<LimitCheckResult> failedChecks, string? recommendedAction = null)
    {
        return new SubscriptionLimitValidationResult
        {
            IsWithinLimits = false, LimitChecks = failedChecks, Message = "One or more limits have been exceeded", RecommendedAction = recommendedAction ?? "Consider upgrading your plan"
        };
    }
}

