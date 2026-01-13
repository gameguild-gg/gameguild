namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation service interface
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>Calculate tax for a transaction</summary>
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get applicable tax rules for a jurisdiction</summary>
    Task<IEnumerable<TaxRule>> GetApplicableTaxRulesAsync(string jurisdictionCode, CustomerType customerType, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);

    /// <summary>Get tax rate for a jurisdiction and product category</summary>
    Task<TaxRate?> GetTaxRateAsync(string jurisdictionCode, TaxType taxType, string? productCategory = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);

    /// <summary>Validate tax exemption</summary>
    Task<bool> ValidateTaxExemptionAsync(Guid customerId, string jurisdictionCode, CancellationToken cancellationToken = default);

    /// <summary>Get all tax jurisdictions</summary>
    Task<IEnumerable<TaxJurisdiction>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Validate VAT number (EU)</summary>
    Task<bool> ValidateVatNumberAsync(string vatNumber, string countryCode, CancellationToken cancellationToken = default);
}
