using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get audit trail by entity
/// </summary>
public sealed record GetAuditTrailQuery(string EntityType, Guid EntityId, int Skip = 0, int Take = 50) : IQuery<List<AuditTrail>>;
