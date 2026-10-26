using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to process a payment
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="SubscriptionId">Subscription ID</param>
/// <param name="Amount">Payment amount</param>
/// <param name="PaymentMethodId">Payment method identifier</param>
/// <param name="Country">
///     Optional ISO 3166-1 alpha-2 billing country (issue #413): feeds the geography-aware
///     gateway routing filters/strategy and the geographic analytics breakdown.
/// </param>
public sealed record ProcessPaymentCommand(
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    string PaymentMethodId,
    string? Country = null) : ICommand<PaymentResult>;
