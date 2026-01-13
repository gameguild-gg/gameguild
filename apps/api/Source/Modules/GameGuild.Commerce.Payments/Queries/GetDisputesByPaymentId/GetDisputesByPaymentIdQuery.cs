using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get disputes by payment ID
/// </summary>
public record GetDisputesByPaymentIdQuery(Guid PaymentId) : IQuery<List<PaymentDispute>>;
