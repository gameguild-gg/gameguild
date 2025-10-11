namespace GameGuild.Modules.Payments.Models;

/// <summary>
///     Applied discount information
/// </summary>
public class AppliedDiscount
{
    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Money Amount { get; init; } = Money.Zero();

    public decimal Percentage { get; init; }

    public DiscountType Type { get; init; }
}

