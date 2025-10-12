namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

/// <summary>
///     Plan feature definition
/// </summary>
public class PlanFeature
{
    /// <summary>
    ///     Feature code/identifier
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the feature
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Feature description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Feature category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    ///     Whether this is a premium feature
    /// </summary>
    public bool IsPremium { get; set; } = false;

    /// <summary>
    ///     Display order within category
    /// </summary>
    public int SortOrder { get; set; } = 0;
}

