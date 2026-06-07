namespace GameGuild.Commerce.Payments;

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
