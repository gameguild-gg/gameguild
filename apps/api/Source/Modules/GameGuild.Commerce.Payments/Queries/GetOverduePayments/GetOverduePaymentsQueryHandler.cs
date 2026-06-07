using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting overdue payments that require collection or retry.
/// </summary>
public sealed class GetOverduePaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetOverduePaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetOverduePaymentsQuery request, CancellationToken cancellationToken)
    {
        var thresholdDays = Math.Max(0, request.OverdueThreshold);
        var cutoff = SystemClock.UtcNow.AddDays(-thresholdDays);

        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Failed)
            .Where(payment => (payment.NextRetryAt != null && payment.NextRetryAt <= cutoff)
                              || (payment.NextRetryAt == null && payment.UpdatedAt <= cutoff));

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(payment => (payment.NextRetryAt ?? payment.UpdatedAt) >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(payment => (payment.NextRetryAt ?? payment.UpdatedAt) <= request.EndDate.Value);
        }

        var payments = await query
            .OrderBy(payment => payment.NextRetryAt ?? payment.UpdatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
