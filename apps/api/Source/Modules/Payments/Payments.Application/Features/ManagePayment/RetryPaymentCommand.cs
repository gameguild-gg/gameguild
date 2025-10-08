using GameGuild.Modules.Payments.Models;
using MediatR;

namespace GameGuild.Modules.Payments.Features.ManagePayment;

/// <summary>
///     Command to retry a failed payment
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public record RetryPaymentCommand(Guid PaymentId) : ICommand<PaymentRetryResult>;

