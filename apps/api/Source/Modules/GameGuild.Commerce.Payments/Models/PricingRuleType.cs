namespace GameGuild.Commerce.Payments;

/// <summary>Pricing rule types</summary>
public enum PricingRuleType
{
    /// <summary>Percentage discount</summary>
    Percentage = 0,

    /// <summary>Fixed amount discount</summary>
    FixedAmount = 1,

    /// <summary>Buy X Get Y discount</summary>
    BuyXGetY = 2,

    /// <summary>Volume discount</summary>
    VolumeDiscount = 3,

    /// <summary>Tiered pricing</summary>
    TieredPricing = 4,

    /// <summary>Bundle discount</summary>
    Bundle = 5
}
