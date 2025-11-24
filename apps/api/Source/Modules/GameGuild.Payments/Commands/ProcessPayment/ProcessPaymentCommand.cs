using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to process a payment
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="SubscriptionId">Subscription ID</param>
/// <param name="Amount">Payment amount</param>
/// <param name="PaymentMethodId">Payment method identifier</param>
public record ProcessPaymentCommand(Guid TenantId, Guid SubscriptionId, decimal Amount, string PaymentMethodId) : ICommand<PaymentResult>;
