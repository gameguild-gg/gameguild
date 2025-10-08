using GameGuild.Shared;
namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
///     Represents an applied discount
/// </summary>
public class AppliedDiscount
{
    /// <summary>
    ///     Discount code or ID
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    ///     Discount description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     Discount amount
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    ///     Discount percentage (if applicable)
    /// </summary>
    public decimal? Percentage { get; init; }

    /// <summary>
    ///     Discount type
    /// </summary>
    public DiscountType Type { get; init; }
}

