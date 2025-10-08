using GameGuild.Shared;
namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
///     Represents a pricing add-on
/// </summary>
public class PricingAddOn
{
    /// <summary>
    ///     Add-on ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     Add-on name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Quantity
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    ///     Unit price
    /// </summary>
    public Money UnitPrice { get; init; } = Money.Zero();

    /// <summary>
    ///     Total price (UnitPrice * Quantity)
    /// </summary>
    public Money TotalPrice
    {
        get => UnitPrice * Quantity;
    }
}

