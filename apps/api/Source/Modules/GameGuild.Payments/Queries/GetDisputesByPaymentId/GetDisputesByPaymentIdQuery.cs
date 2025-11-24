using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get disputes by payment ID
/// </summary>
public record GetDisputesByPaymentIdQuery(Guid PaymentId) : IQuery<List<PaymentDispute>>;
