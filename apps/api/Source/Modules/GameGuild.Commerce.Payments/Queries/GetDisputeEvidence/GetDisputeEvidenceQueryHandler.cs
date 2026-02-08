using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetDisputeEvidenceQuery
/// </summary>
public sealed class GetDisputeEvidenceQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputeEvidenceQuery, List<DisputeEvidence>>
{
    public async Task<List<DisputeEvidence>> Handle(GetDisputeEvidenceQuery request, CancellationToken cancellationToken) { return await disputeService.GetDisputeEvidenceAsync(request.DisputeId, cancellationToken).ConfigureAwait(false); }
}
