using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get disputes by user ID
/// </summary>
public record GetDisputesByUserIdQuery(Guid UserId, int Skip = 0, int Take = 50) : IQuery<List<PaymentDispute>>;
