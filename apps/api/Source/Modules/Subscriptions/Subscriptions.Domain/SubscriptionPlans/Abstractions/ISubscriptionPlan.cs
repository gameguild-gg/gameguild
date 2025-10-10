
namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Abstractions;

/// <summary>
///     Interface for subscription plan entities
/// </summary>
public interface ISubscriptionPlan
{
    /// <summary>
    ///     Unique identifier
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Display name of the plan
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     URL-friendly slug
    /// </summary>
    string Slug { get; }

    /// <summary>
    ///     Plan description
    /// </summary>
    string? Description { get; }

    /// <summary>
    ///     Monthly price in smallest currency unit
    /// </summary>
    long MonthlyPriceInCents { get; }

    /// <summary>
    ///     Annual price in smallest currency unit
    /// </summary>
    long? AnnualPriceInCents { get; }

    /// <summary>
    ///     Currency code
    /// </summary>
    string Currency { get; }

    /// <summary>
    ///     Whether the plan is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     Maximum users allowed
    /// </summary>
    int? MaxUsers { get; }

    /// <summary>
    ///     Maximum storage in MB
    /// </summary>
    long? MaxStorageMb { get; }

    /// <summary>
    ///     Maximum API calls per month
    /// </summary>
    long? MaxApiCallsPerMonth { get; }

    /// <summary>
    ///     Gets the monthly price as Money
    /// </summary>
    Money GetMonthlyPrice();

    /// <summary>
    ///     Gets the annual price as Money
    /// </summary>
    Money? GetAnnualPrice();

    /// <summary>
    ///     Checks if plan allows user count
    /// </summary>
    bool AllowsUserCount(int userCount);

    /// <summary>
    ///     Checks if plan allows storage amount
    /// </summary>
    bool AllowsStorage(long storageMb);

    /// <summary>
    ///     Checks if plan allows API calls
    /// </summary>
    bool AllowsApiCalls(long apiCalls);
}

