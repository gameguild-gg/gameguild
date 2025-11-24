using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get evidence for a dispute
/// </summary>
public record GetDisputeEvidenceQuery(Guid DisputeId) : IQuery<List<DisputeEvidence>>;
