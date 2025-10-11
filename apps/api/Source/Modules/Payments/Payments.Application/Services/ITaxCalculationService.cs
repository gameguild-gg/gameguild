using GameGuild.Modules.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Payments.Application.Services;

/// <summary>
///     Tax calculation service interface
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    ///     Calculate tax for a transaction
    /// </summary>
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get applicable tax rules for a jurisdiction
    /// </summary>
    Task<IEnumerable<TaxRule>> GetApplicableTaxRulesAsync(
        string jurisdictionCode,
        CustomerType customerType,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tax rate for a jurisdiction and product category
    /// </summary>
    Task<TaxRate?> GetTaxRateAsync(
        string jurisdictionCode,
        TaxType taxType,
        string? productCategory = null,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validate tax exemption
    /// </summary>
    Task<bool> ValidateTaxExemptionAsync(
        Guid customerId,
        string jurisdictionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all tax jurisdictions
    /// </summary>
    Task<IEnumerable<TaxJurisdiction>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validate VAT number (EU)
    /// </summary>
    Task<bool> ValidateVatNumberAsync(string vatNumber, string countryCode, CancellationToken cancellationToken = default);
}

/// <summary>
///     Tax calculation request
/// </summary>
public class TaxCalculationRequest
{
    public required string JurisdictionCode { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required CustomerType CustomerType { get; init; }
    public string? ProductCategory { get; init; }
    public string? CustomerVatNumber { get; init; }
    public bool IsTaxInclusive { get; init; }
    public DateTime TransactionDate { get; init; } = DateTime.UtcNow;
    public List<string> ApplicableExemptions { get; init; } = new();
}

/// <summary>
///     Tax calculation result
/// </summary>
public class TaxCalculationResult
{
    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal EffectiveTaxRate { get; init; }
    public string JurisdictionCode { get; init; } = string.Empty;
    public string JurisdictionName { get; init; } = string.Empty;
    public TaxType TaxType { get; init; }
    public string TaxDescription { get; init; } = string.Empty;
    public bool IsTaxExempt { get; init; }
    public bool IsReverseCharge { get; init; }
    public List<TaxBreakdown> TaxBreakdowns { get; init; } = new();
    public string? ExemptionReason { get; init; }
}

/// <summary>
///     Individual tax breakdown (for compound/multiple taxes)
/// </summary>
public class TaxBreakdown
{
    public TaxType TaxType { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string JurisdictionCode { get; init; } = string.Empty;
}
