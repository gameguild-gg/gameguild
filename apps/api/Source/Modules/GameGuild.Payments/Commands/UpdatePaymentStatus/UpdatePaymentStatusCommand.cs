using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Commands;

/// <summary>
///     Command to update payment status
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
/// <param name="Status">New payment status</param>
/// <param name="TransactionId">External transaction ID</param>
public record UpdatePaymentStatusCommand(Guid PaymentId, PaymentStatus Status, string? TransactionId = null) : ICommand<bool>;
