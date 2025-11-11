using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for getting failed payments
/// </summary>
public sealed class GetFailedPaymentsQueryHandler : IQueryHandler<GetFailedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetFailedPaymentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement failed payments retrieval logic
        // 1. Query database for failed payments
        // 2. Apply tenant filtering if specified
        // 3. Apply date range filtering
        // 4. Return payment results

        await Task.Delay(100, cancellationToken); // Placeholder

        return new List<PaymentResult>();
    }
}
