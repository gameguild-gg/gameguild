using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting failed payments.
/// </summary>
public sealed class GetFailedPaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetFailedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetFailedPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Failed);

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        var payments = await query
            .OrderBy(payment => payment.NextRetryAt ?? payment.UpdatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
