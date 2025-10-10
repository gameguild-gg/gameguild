using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for UpdateDisputeStatusCommand</summary>
public class UpdateDisputeStatusHandler : IRequestHandler<UpdateDisputeStatusCommand>
{
    private readonly IDisputeService _disputeService;

    public UpdateDisputeStatusHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task Handle(UpdateDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        await _disputeService.UpdateDisputeStatusAsync(
            request.DisputeId,
            request.NewStatus,
            request.DueDate,
            cancellationToken);
    }
}
