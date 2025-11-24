namespace GameGuild.Subscriptions.DTOs;

/// <summary>
///     DTO for updating payment method
/// </summary>
public record UpdatePaymentMethodDto
{
    /// <summary>
    ///     Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; init; }

    // TODO: Add payment method properties when Payment module is implemented
    // - PaymentMethodType (CreditCard, BankAccount, etc.)
    // - Last4Digits
    // - ExpiryDate
    // - BillingAddress
}
