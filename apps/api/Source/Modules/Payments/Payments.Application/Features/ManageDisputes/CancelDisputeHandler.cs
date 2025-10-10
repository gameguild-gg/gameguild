using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for CancelDisputeCommand</summary>
public class CancelDisputeHandler : IRequestHandler<CancelDisputeCommand>
{
    private readonly IDisputeService _disputeService;

    public CancelDisputeHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task Handle(CancelDisputeCommand request, CancellationToken cancellationToken)
    {
        await _disputeService.CancelDisputeAsync(request.DisputeId, request.Reason, cancellationToken);
    }
}
