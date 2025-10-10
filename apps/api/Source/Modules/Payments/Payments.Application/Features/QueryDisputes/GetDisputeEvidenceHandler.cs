using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Handler for GetDisputeEvidenceQuery</summary>
public class GetDisputeEvidenceHandler : IRequestHandler<GetDisputeEvidenceQuery, List<DisputeEvidence>>
{
    private readonly IDisputeService _disputeService;

    public GetDisputeEvidenceHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<List<DisputeEvidence>> Handle(GetDisputeEvidenceQuery request, CancellationToken cancellationToken)
    {
        return await _disputeService.GetDisputeEvidenceAsync(request.DisputeId, cancellationToken);
    }
}
