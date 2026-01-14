using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting failed payments.
/// </summary>
/// <remarks>
///     Failed payment tracking integrates with:
///     - Subscription payment failure history
///     - Payment gateway decline records
///     - Invoice payment attempts
/// </remarks>
public sealed class GetFailedPaymentsQueryHandler : IQueryHandler<GetFailedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public Task<IEnumerable<PaymentResult>> Handle(GetFailedPaymentsQuery request, CancellationToken cancellationToken)
    {
        // Failed payment queries should integrate with:
        // - ISubscriptionService for subscription payment failures
        // - IFinancialLedgerRepository for failed transaction records
        // - Payment gateway failure history
        //
        // Returns empty collection until failure tracking is fully implemented.

        return Task.FromResult<IEnumerable<PaymentResult>>(new List<PaymentResult>());
    }
}
