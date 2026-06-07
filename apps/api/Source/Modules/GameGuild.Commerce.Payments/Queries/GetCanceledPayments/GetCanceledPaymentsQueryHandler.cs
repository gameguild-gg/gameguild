using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting canceled payments.
/// </summary>
public sealed class GetCanceledPaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetCanceledPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetCanceledPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Cancelled);

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            query = query.Where(payment => payment.CancellationReason != null
                                           && payment.CancellationReason.Contains(request.CancellationReason));
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(payment => payment.CancelledAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(payment => payment.CancelledAt <= request.EndDate.Value);
        }

        var payments = await query
            .OrderByDescending(payment => payment.CancelledAt ?? payment.UpdatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
