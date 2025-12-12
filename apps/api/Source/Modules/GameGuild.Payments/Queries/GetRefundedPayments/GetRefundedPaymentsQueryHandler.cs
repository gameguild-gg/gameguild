using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries.GetRefundedPayments;

/// <summary>
///     Handler for getting refunded payments
/// </summary>
public class GetRefundedPaymentsQueryHandler : IQueryHandler<GetRefundedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetRefundedPaymentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual payment repository query for refunded payments
        // This should query payments with status = "refunded" or partial refunds
        await Task.CompletedTask;

        return [];
    }
}
