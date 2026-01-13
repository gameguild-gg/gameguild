using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get failed payments that need retry
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
public record GetFailedPaymentsQuery(Guid? TenantId = null) : IQuery<IEnumerable<PaymentResult>>;
