namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Discount type enumeration
/// </summary>
public enum DiscountType
{
    /// <summary>
    ///     Fixed amount discount
    /// </summary>
    FixedAmount,

    /// <summary>
    ///     Percentage discount
    /// </summary>
    Percentage,

    /// <summary>
    ///     Free trial extension
    /// </summary>
    FreeTrial,

    /// <summary>
    ///     Buy one get one
    /// </summary>
    Bogo,

    /// <summary>
    ///     Custom discount type
    /// </summary>
    Custom
}
