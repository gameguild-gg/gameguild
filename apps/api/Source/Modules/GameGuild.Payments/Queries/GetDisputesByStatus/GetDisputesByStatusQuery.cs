using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get disputes by status
/// </summary>
public record GetDisputesByStatusQuery(DisputeStatus Status, int Skip = 0, int Take = 50) : IQuery<List<PaymentDispute>>;
