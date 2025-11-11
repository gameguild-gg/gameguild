using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get failed payments that need retry
/// </summary>
/// <param name="TenantId">Optional tenant ID filter</param>
public record GetFailedPaymentsQuery(Guid? TenantId = null) : IQuery<IEnumerable<PaymentResult>>;
