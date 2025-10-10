using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Command to update dispute status</summary>
public record UpdateDisputeStatusCommand(
    Guid DisputeId,
    DisputeStatus NewStatus,
    DateTime? DueDate = null
) : IRequest;
