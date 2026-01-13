using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to retry a failed payment
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public record RetryPaymentCommand(Guid PaymentId) : ICommand<PaymentRetryResult>;
