using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get evidence for a dispute
/// </summary>
public record GetDisputeEvidenceQuery(Guid DisputeId) : IQuery<List<DisputeEvidence>>;
