
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Tax calculation details
/// </summary>
public abstract class TaxDetails
{
    /// <summary>
    ///     Tax rate applied
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    ///     Tax amount
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    ///     Tax region/jurisdiction
    /// </summary>
    public string? Jurisdiction { get; init; }

    /// <summary>
    ///     Tax type (VAT, Sales Tax, etc.)
    /// </summary>
    public string? TaxType { get; init; }
}
