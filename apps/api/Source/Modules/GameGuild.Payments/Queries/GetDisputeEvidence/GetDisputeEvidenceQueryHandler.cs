using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetDisputeEvidenceQuery
/// </summary>
public sealed class GetDisputeEvidenceQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputeEvidenceQuery, List<DisputeEvidence>>
{
    public async Task<List<DisputeEvidence>> Handle(GetDisputeEvidenceQuery request, CancellationToken cancellationToken) { return await disputeService.GetDisputeEvidenceAsync(request.DisputeId, cancellationToken); }
}
