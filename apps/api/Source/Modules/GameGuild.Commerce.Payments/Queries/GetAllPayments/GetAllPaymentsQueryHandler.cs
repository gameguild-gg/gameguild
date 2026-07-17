using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting all payments with filtering and pagination.
/// </summary>
public sealed class GetAllPaymentsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetAllPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetAllPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Payment>().AsNoTracking();

        if (request.TenantId.HasValue)
        {
            query = query.Where(payment => payment.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var normalizedStatus = request.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)
                ? nameof(PaymentStatus.Succeeded)
                : request.Status;

            if (!Enum.TryParse<PaymentStatus>(normalizedStatus, true, out var status))
            {
                throw new ArgumentException($"Unknown payment status '{request.Status}'.", nameof(request));
            }

            query = query.Where(payment => payment.Status == status);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(payment => payment.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(payment => payment.CreatedAt <= request.EndDate.Value);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var payments = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentQueryMapper.ToResult).ToList();
    }
}
