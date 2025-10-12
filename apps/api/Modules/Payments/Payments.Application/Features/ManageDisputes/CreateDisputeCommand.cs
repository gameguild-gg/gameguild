using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Command to create a new payment dispute</summary>
public record CreateDisputeCommand(
    Guid PaymentId,
    Guid UserId,
    DisputeType Type,
    decimal Amount,
    string Reason,
    string? Description = null
) : IRequest<PaymentDispute>;
