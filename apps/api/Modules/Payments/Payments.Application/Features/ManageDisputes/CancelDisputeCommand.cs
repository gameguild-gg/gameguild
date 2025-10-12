using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Command to cancel a dispute</summary>
public record CancelDisputeCommand(
    Guid DisputeId,
    string Reason
) : IRequest;
