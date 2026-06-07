using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting payment history.
/// </summary>
public sealed class GetPaymentHistoryQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPaymentHistoryQuery, List<PaymentHistoryResult>>
{
    public async Task<List<PaymentHistoryResult>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>().AsNoTracking();

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(payment => payment.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(payment => payment.CreatedAt <= request.EndDate.Value);
        }

        var payments = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

        if (request.UserId.HasValue)
        {
            payments = payments
                .Where(payment => PaymentQueryMapper.TryGetUserId(payment.Metadata) == request.UserId.Value)
                .ToList();
        }

        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        return payments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PaymentQueryMapper.ToHistoryResult)
            .ToList();
    }
}
