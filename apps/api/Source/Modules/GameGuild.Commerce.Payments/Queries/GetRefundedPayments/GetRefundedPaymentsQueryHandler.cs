using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting refunded payments.
/// </summary>
public sealed class GetRefundedPaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetRefundedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetRefundedPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Refunded || payment.RefundedAmount > 0m);

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.RefundReason))
        {
            query = query.Where(payment => payment.RefundReason != null
                                           && payment.RefundReason.Contains(request.RefundReason));
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(payment => payment.RefundedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(payment => payment.RefundedAt <= request.EndDate.Value);
        }

        var payments = await query
            .OrderByDescending(payment => payment.RefundedAt ?? payment.UpdatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
