using GameGuild.Modules.Payments.Models;
using MediatR;

namespace GameGuild.Modules.Payments.Features.GetPayment;

/// <summary>
///     Query to get payment by ID
/// </summary>
/// <param name="PaymentId">Payment unique identifier</param>
public record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentResult?>;

