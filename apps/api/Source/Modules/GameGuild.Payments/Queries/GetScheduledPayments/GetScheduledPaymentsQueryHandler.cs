using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries.GetScheduledPayments;

/// <summary>
///     Handler for getting scheduled payments
/// </summary>
public class GetScheduledPaymentsQueryHandler : IQueryHandler<GetScheduledPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetScheduledPaymentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual payment repository query for scheduled payments
        // This should query payments with status = "scheduled" or future execution dates
        await Task.CompletedTask;

        return [];
    }
}
