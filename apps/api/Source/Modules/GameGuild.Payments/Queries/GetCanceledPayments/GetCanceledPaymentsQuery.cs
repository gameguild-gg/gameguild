using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get canceled payments
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
/// <param name="CancellationReason">Optional cancellation reason filter</param>
/// <param name="StartDate">Optional start date filter for cancellation date</param>
/// <param name="EndDate">Optional end date filter for cancellation date</param>
public record GetCanceledPaymentsQuery(Guid? TenantId = null, string? CancellationReason = null, DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<IEnumerable<PaymentResult>>;
