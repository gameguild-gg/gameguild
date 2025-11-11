using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to create a payment dispute
/// </summary>
public sealed record CreateDisputeCommand(Guid PaymentId, Guid UserId, DisputeType Type, decimal Amount, string Reason, string Description) : ICommand<PaymentDispute>;
