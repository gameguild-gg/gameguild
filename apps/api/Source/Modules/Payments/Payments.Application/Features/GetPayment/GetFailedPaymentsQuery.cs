using GameGuild.Modules.Payments.Models;
using MediatR;

namespace GameGuild.Modules.Payments.Features.GetPayment;

/// <summary>
///     Query to get failed payments that need retry
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
public record GetFailedPaymentsQuery(Guid? TenantId = null) : IQuery<IEnumerable<PaymentResult>>;

