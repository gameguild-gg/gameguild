using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Command to resolve a dispute</summary>
public record ResolveDisputeCommand(
    Guid DisputeId,
    DisputeResolution Resolution,
    string Notes,
    Guid ResolvedBy
) : IRequest;
