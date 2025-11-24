using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to process a refund for a payment
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
/// <param name="Amount">Refund amount</param>
/// <param name="Reason">Reason for the refund</param>
public sealed record ProcessRefundCommand(Guid PaymentId, decimal Amount, string Reason) : ICommand<ProcessRefundResult>;
