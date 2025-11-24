namespace GameGuild.Modules.Products.Domain.Enums;

/// <summary>
/// Represents how a user acquired access to a product
/// </summary>
public enum ProductAcquisitionType
{
    /// <summary>Direct purchase</summary>
    Purchase = 0,

    /// <summary>Access via subscription</summary>
    Subscription = 1,

    /// <summary>Granted by administrator or instructor</summary>
    Grant = 2,

    /// <summary>Access via promotional code</summary>
    PromoCode = 3,

    /// <summary>Access via bundle purchase</summary>
    Bundle = 4,

    /// <summary>Trial or free access</summary>
    Trial = 5,

    /// <summary>Access via affiliate/referral</summary>
    Referral = 6
}

/// <summary>
/// Represents the current access status for a user's product
/// </summary>
public enum ProductAccessStatus
{
    /// <summary>Access is active and valid</summary>
    Active = 0,

    /// <summary>Access has expired</summary>
    Expired = 1,

    /// <summary>Access has been revoked</summary>
    Revoked = 2,

    /// <summary>Access is suspended</summary>
    Suspended = 3,

    /// <summary>Access is pending activation</summary>
    Pending = 4,

    /// <summary>Access was cancelled</summary>
    Cancelled = 5
}

/// <summary>
/// Represents the type of promo code discount
/// </summary>
public enum PromoCodeType
{
    /// <summary>Percentage discount (e.g., 20% off)</summary>
    PercentageOff = 0,

    /// <summary>Fixed amount discount (e.g., $10 off)</summary>
    FixedAmountOff = 1,

    /// <summary>Free trial period</summary>
    FreeTrial = 2,

    /// <summary>Buy one get one free</summary>
    BuyOneGetOne = 3,

    /// <summary>Free shipping</summary>
    FreeShipping = 4
}
