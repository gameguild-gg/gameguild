using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to retry a failed payment
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public record RetryPaymentCommand(Guid PaymentId) : ICommand<PaymentRetryResult>;
