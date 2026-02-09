using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get all payments with filtering and pagination
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
/// <param name="Status">Optional payment status filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="Page">Page number for pagination</param>
/// <param name="PageSize">Page size for pagination</param>
public sealed record GetAllPaymentsQuery(Guid? TenantId = null, string? Status = null, DateTime? StartDate = null, DateTime? EndDate = null, int Page = 1, int PageSize = 20) : IQuery<IEnumerable<PaymentResult>>;
