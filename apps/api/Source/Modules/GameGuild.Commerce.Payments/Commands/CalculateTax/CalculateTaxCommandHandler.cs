using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CalculateTaxCommand
/// </summary>
public sealed class CalculateTaxCommandHandler(ITaxCalculationService taxCalculationService) : ICommandHandler<CalculateTaxCommand, TaxCalculationResult>
{
    public async Task<TaxCalculationResult> Handle(CalculateTaxCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Parse CustomerType string to enum
        if (!Enum.TryParse(request.CustomerType, true, out CustomerType customerType))
        {
            customerType = CustomerType.B2C; // Default fallback to B2C
        }

        var taxRequest = new TaxCalculationRequest
        {
            JurisdictionCode = request.JurisdictionCode,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerType = customerType,
            ProductCategory = request.ProductCategory,
            CustomerVatNumber = request.CustomerVatNumber,
            IsTaxInclusive = request.IsTaxInclusive,
            TransactionDate = request.TransactionDate ?? SystemClock.UtcNow,
            ApplicableExemptions = request.ApplicableExemptions?.ToList() ?? new List<string>()
        };

        return await taxCalculationService.CalculateTaxAsync(taxRequest, cancellationToken).ConfigureAwait(false);
    }
}
