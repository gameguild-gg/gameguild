using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get payment by ID
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public sealed record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentResult?>;
