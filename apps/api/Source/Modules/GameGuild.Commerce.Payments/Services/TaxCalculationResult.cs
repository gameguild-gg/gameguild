namespace GameGuild.Commerce.Payments;

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

    public List<TaxBreakdown> TaxBreakdowns { get; init; } = new List<TaxBreakdown>();

    public string? ExemptionReason { get; init; }
}
