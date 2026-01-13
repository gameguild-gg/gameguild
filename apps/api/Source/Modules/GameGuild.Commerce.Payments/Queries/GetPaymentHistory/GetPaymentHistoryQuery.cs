using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get payment history
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="PageNumber">Page number for pagination</param>
/// <param name="PageSize">Page size for pagination</param>
/// <param name="IsAdminRequest">Indicates if this is an admin request</param>
public record GetPaymentHistoryQuery(
    Guid? UserId = null,
    Guid? TenantId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int PageNumber = 1,
    int PageSize = 20,
    bool IsAdminRequest = false
) : IQuery<List<PaymentHistoryResult>>;
