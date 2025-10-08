using GameGuild.Modules.Payments.Models;
using MediatR;

namespace GameGuild.Modules.Payments.Features.GetPayment;

/// <summary>
///     Query to get payment history for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
public record GetPaymentHistoryQuery(
    Guid TenantId,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IQuery<IEnumerable<PaymentResult>>;

