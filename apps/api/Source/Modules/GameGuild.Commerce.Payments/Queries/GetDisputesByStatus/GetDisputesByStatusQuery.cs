using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get disputes by status
/// </summary>
public sealed record GetDisputesByStatusQuery(DisputeStatus Status, int Skip = 0, int Take = 50) : IQuery<List<PaymentDispute>>;
