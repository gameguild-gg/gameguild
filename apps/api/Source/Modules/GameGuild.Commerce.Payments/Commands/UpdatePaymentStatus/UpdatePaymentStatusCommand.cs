using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to update payment status
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
/// <param name="Status">New payment status</param>
/// <param name="TransactionId">External transaction ID</param>
public sealed record UpdatePaymentStatusCommand(Guid PaymentId, PaymentStatus Status, string? TransactionId = null) : ICommand<bool>;
