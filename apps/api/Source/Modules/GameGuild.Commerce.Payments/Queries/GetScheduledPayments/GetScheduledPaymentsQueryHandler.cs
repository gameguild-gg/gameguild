using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting scheduled payments.
/// </summary>
public sealed class GetScheduledPaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetScheduledPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetScheduledPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Failed && payment.NextRetryAt != null);

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (request.ScheduledDate.HasValue)
        {
            var start = request.ScheduledDate.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(payment => payment.NextRetryAt >= start && payment.NextRetryAt < end);
        }
        else
        {
            query = query.Where(payment => payment.NextRetryAt >= SystemClock.UtcNow);
        }

        var payments = await query
            .OrderBy(payment => payment.NextRetryAt)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
