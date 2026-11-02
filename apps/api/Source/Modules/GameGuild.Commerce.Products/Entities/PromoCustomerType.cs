namespace GameGuild.Commerce.Products;

/// <summary>
/// Customer relationship state a promo code is eligible for (issue #418).
/// The default (<see cref="Any"/>) keeps unrestricted codes working unchanged.
/// </summary>
public enum PromoCustomerType
{
    /// <summary>No customer-state restriction; every customer may redeem.</summary>
    Any = 0,

    /// <summary>First-time customers only: no prior purchase or promo redemption.</summary>
    New = 1,

    /// <summary>Customers with an active purchase relationship.</summary>
    Existing = 2,

    /// <summary>Churned customers only (win-back): prior purchases, none currently active.</summary>
    Churned = 3
}
