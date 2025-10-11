namespace GameGuild.Modules.Tenants;

/// <summary>
/// Billing provider options for tenant billing integrations.
/// </summary>
public enum TenantBillingProvider
{
    /// <summary>
    /// Stripe payment platform
    /// </summary>
    Stripe = 1,

    /// <summary>
    /// PayPal payment platform
    /// </summary>
    PayPal = 2,

    /// <summary>
    /// Manual billing/invoicing
    /// </summary>
    Manual = 3,

    /// <summary>
    /// Custom internal billing system
    /// </summary>
    Custom = 4,

    /// <summary>
    /// Razorpay payment platform
    /// </summary>
    Razorpay = 5,

    /// <summary>
    /// Square payment platform
    /// </summary>
    Square = 6,

    /// <summary>
    /// Braintree payment platform
    /// </summary>
    Braintree = 7,

    /// <summary>
    /// Authorize.Net payment platform
    /// </summary>
    AuthorizeNet = 8
}
