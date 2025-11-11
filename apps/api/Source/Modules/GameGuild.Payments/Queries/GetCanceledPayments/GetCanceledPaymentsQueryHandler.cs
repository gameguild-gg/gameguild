using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries.GetCanceledPayments;

/// <summary>
///     Handler for getting canceled payments
/// </summary>
public class GetCanceledPaymentsQueryHandler : IQueryHandler<GetCanceledPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetCanceledPaymentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual payment repository query for canceled payments
        // This should query payments with status = "canceled" or "cancelled"
        await Task.CompletedTask;

        return [];
    }
}
