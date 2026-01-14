namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     DTO for updating payment method
/// </summary>
public record UpdatePaymentMethodDto
{
    /// <summary>
    ///     Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; init; }

    /// <summary>
    ///     Type of payment method (CreditCard, DebitCard, BankAccount, PayPal, etc.)
    /// </summary>
    public PaymentMethodType PaymentMethodType { get; init; }

    /// <summary>
    ///     Last 4 digits of card or account number (for display purposes)
    /// </summary>
    public string? Last4Digits { get; init; }

    /// <summary>
    ///     Card brand (Visa, Mastercard, Amex, etc.) - applicable for card payments
    /// </summary>
    public string? CardBrand { get; init; }

    /// <summary>
    ///     Card expiry month (1-12)
    /// </summary>
    public int? ExpiryMonth { get; init; }

    /// <summary>
    ///     Card expiry year (4 digits)
    /// </summary>
    public int? ExpiryYear { get; init; }

    /// <summary>
    ///     Name on the card or account
    /// </summary>
    public string? CardholderName { get; init; }

    /// <summary>
    ///     Billing address for the payment method
    /// </summary>
    public BillingAddressDto? BillingAddress { get; init; }

    /// <summary>
    ///     External payment method ID from payment gateway (e.g., Stripe payment method ID)
    /// </summary>
    public string? ExternalPaymentMethodId { get; init; }

    /// <summary>
    ///     Whether this should be set as the default payment method
    /// </summary>
    public bool SetAsDefault { get; init; }
}

/// <summary>
///     Types of payment methods supported
/// </summary>
public enum PaymentMethodType
{
    /// <summary>Credit card payment</summary>
    CreditCard,
    
    /// <summary>Debit card payment</summary>
    DebitCard,
    
    /// <summary>Bank account (ACH) transfer</summary>
    BankAccount,
    
    /// <summary>PayPal account</summary>
    PayPal,
    
    /// <summary>Apple Pay</summary>
    ApplePay,
    
    /// <summary>Google Pay</summary>
    GooglePay,
    
    /// <summary>Cryptocurrency wallet</summary>
    Crypto,
    
    /// <summary>Other payment method</summary>
    Other
}

/// <summary>
///     DTO for billing address
/// </summary>
public record BillingAddressDto
{
    /// <summary>Street address line 1</summary>
    public string? Line1 { get; init; }
    
    /// <summary>Street address line 2</summary>
    public string? Line2 { get; init; }
    
    /// <summary>City</summary>
    public string? City { get; init; }
    
    /// <summary>State or province</summary>
    public string? State { get; init; }
    
    /// <summary>Postal or ZIP code</summary>
    public string? PostalCode { get; init; }
    
    /// <summary>Country (ISO 3166-1 alpha-2 code)</summary>
    public string? Country { get; init; }
}

