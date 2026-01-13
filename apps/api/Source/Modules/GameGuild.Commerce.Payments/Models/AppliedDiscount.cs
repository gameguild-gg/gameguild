using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Applied discount information
/// </summary>
public abstract class AppliedDiscount
{
    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Money Amount { get; init; } = Money.Zero();

    public decimal Percentage { get; init; }

    public DiscountType Type { get; init; }
}
