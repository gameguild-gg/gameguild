using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for getting refunded payments.
/// </summary>
/// <remarks>
///     Refund tracking is typically managed via:
///     - FinancialLedgerEntry with refund transaction type
///     - Payment gateway refund records
/// </remarks>
public sealed class GetRefundedPaymentsQueryHandler : IQueryHandler<GetRefundedPaymentsQuery, IEnumerable<PaymentResult>>
{
    public Task<IEnumerable<PaymentResult>> Handle(GetRefundedPaymentsQuery request, CancellationToken cancellationToken)
    {
        // Refund queries should integrate with:
        // - IFinancialLedgerRepository for refund ledger entries
        // - Payment gateway refund history
        //
        // Returns empty collection until refund tracking is fully implemented.
        
        return Task.FromResult<IEnumerable<PaymentResult>>([]);
    }
}
