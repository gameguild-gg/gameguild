using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries.GetAllPayments;

/// <summary>
///     Handler for getting all payments with filtering and pagination
/// </summary>
public class GetAllPaymentsQueryHandler : IQueryHandler<GetAllPaymentsQuery, IEnumerable<PaymentResult>>
{
    public async Task<IEnumerable<PaymentResult>> Handle(GetAllPaymentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual payment repository query
        // This is a placeholder implementation
        await Task.CompletedTask;

        return [];
    }
}
