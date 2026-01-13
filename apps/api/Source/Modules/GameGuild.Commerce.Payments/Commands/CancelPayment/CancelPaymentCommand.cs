using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to cancel a payment transaction
/// </summary>
/// <param name="PaymentId">The unique identifier of the payment to cancel</param>
/// <param name="CancellationReason">The reason for canceling the payment</param>
/// <param name="CanceledBy">The user ID who canceled the payment</param>
public record CancelPaymentCommand(Guid PaymentId, string CancellationReason, Guid? CanceledBy = null) : ICommand<PaymentCancellationResult>;
