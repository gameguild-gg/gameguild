using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get refunded payments
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
/// <param name="RefundReason">Optional refund reason filter</param>
/// <param name="StartDate">Optional start date filter for refund processing date</param>
/// <param name="EndDate">Optional end date filter for refund processing date</param>
public record GetRefundedPaymentsQuery(Guid? TenantId = null, string? RefundReason = null, DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<IEnumerable<PaymentResult>>;
