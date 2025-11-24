namespace GameGuild.Subscriptions.Models;

/// <summary>
///     Subscription plan limits
/// </summary>
public class SubscriptionPlanLimits
{
    /// <summary>
    ///     Maximum allowed users
    /// </summary>
    public int MaxUsers { get; init; }

    /// <summary>
    ///     Maximum storage in MB
    /// </summary>
    public long MaxStorageMb { get; init; }

    /// <summary>
    ///     Maximum API calls per month
    /// </summary>
    public long MaxApiCallsPerMonth { get; init; }

    /// <summary>
    ///     Whether unlimited users are allowed
    /// </summary>
    public bool UnlimitedUsers { get; init; }

    /// <summary>
    ///     Whether unlimited storage is allowed
    /// </summary>
    public bool UnlimitedStorage { get; init; }

    /// <summary>
    ///     Whether unlimited API calls are allowed
    /// </summary>
    public bool UnlimitedApiCalls { get; init; }
}
