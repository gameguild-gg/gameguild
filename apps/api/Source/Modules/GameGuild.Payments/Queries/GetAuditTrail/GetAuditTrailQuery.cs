using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get audit trail by entity
/// </summary>
public record GetAuditTrailQuery(string EntityType, Guid EntityId, int Skip = 0, int Take = 50) : IQuery<List<AuditTrail>>;
