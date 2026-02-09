using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get disputes by user ID
/// </summary>
public sealed record GetDisputesByUserIdQuery(Guid UserId, int Skip = 0, int Take = 50) : IQuery<List<PaymentDispute>>;
