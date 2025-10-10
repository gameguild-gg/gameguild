using GameGuild.Modules.Payments.Models;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Features.ProcessPayment;

/// <summary>
///     Command to process a refund
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
/// <param name="Amount">Refund amount (null for full refund)</param>
/// <param name="Reason">Reason for refund</param>
public record ProcessRefundCommand(
    Guid PaymentId,
    decimal? Amount = null,
    string? Reason = null) : ICommand<PaymentResult>;

