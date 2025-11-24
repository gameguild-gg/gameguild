using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get scheduled payments for future execution
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
/// <param name="ScheduledDate">Optional specific scheduled date filter</param>
public record GetScheduledPaymentsQuery(Guid? TenantId = null, DateTime? ScheduledDate = null) : IQuery<IEnumerable<PaymentResult>>;
