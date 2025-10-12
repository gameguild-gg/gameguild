using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for ResolveDisputeCommand</summary>
public class ResolveDisputeHandler : ICommandHandler<ResolveDisputeCommand, Unit>
{
    private readonly IDisputeService _disputeService;

    public ResolveDisputeHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<Unit> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        await _disputeService.ResolveDisputeAsync(
            request.DisputeId,
            request.Resolution,
            request.Notes,
            request.ResolvedBy,
            cancellationToken);

        return Unit.Value;
    }
}
