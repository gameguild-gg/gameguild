using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get payment by ID
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentResult?>;
