using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;
using MediatR;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for UpdateDisputeStatusCommand</summary>
public class UpdateDisputeStatusHandler : IRequestHandler<UpdateDisputeStatusCommand, Unit>
{
    private readonly IDisputeService _disputeService;

    public UpdateDisputeStatusHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<Unit> Handle(UpdateDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        await _disputeService.UpdateDisputeStatusAsync(
            request.DisputeId,
            request.NewStatus,
            request.DueDate,
            cancellationToken);

        return Unit.Value;
    }
}
