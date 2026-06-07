using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting payment by ID.
/// </summary>
public sealed class GetPaymentByIdQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPaymentByIdQuery, PaymentResult?>
{
    public async Task<PaymentResult?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await context.Set<Payment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.PaymentId, cancellationToken);

        return payment is null ? null : PaymentQueryMapper.ToResult(payment);
    }
}
