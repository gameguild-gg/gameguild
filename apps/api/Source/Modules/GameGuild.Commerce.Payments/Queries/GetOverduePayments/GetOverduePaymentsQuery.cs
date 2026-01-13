using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get overdue payments that require attention or collection
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
/// <param name="OverdueThreshold">Number of days past due date to consider overdue</param>
/// <param name="StartDate">Optional start date filter for original payment due date</param>
/// <param name="EndDate">Optional end date filter for original payment due date</param>
public record GetOverduePaymentsQuery(Guid? TenantId = null, int OverdueThreshold = 30, DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<IEnumerable<PaymentResult>>;
