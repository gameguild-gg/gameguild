namespace GameGuild.Modules.Payments.Domain.Entities;

/// <summary>
///     Represents a tax rate for a specific product category in a jurisdiction
/// </summary>
public class TaxRate : EntityBase
{
    /// <summary>
    ///     Tax jurisdiction ID
    /// </summary>
    public Guid TaxJurisdictionId { get; set; }

    /// <summary>
    ///     Tax jurisdiction navigation
    /// </summary>
    public TaxJurisdiction TaxJurisdiction { get; set; } = null!;

    /// <summary>
    ///     Tax type (VAT, GST, Sales Tax, etc.)
    /// </summary>
    public TaxType TaxType { get; set; }

    /// <summary>
    ///     Tax rate (percentage, e.g., 20.0 for 20%)
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    ///     Product category this rate applies to (null = all products)
    /// </summary>
    public string? ProductCategory { get; set; }

    /// <summary>
    ///     Effective start date
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    ///     Effective end date (null = indefinite)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    ///     Is this rate currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Minimum taxable amount
    /// </summary>
    public decimal? MinimumTaxableAmount { get; set; }

    /// <summary>
    ///     Maximum taxable amount
    /// </summary>
    public decimal? MaximumTaxableAmount { get; set; }

    /// <summary>
    ///     Tax description (e.g., "Standard VAT", "Reduced VAT")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Check if rate is currently effective
    /// </summary>
    public bool IsEffective(DateTime date)
    {
        return IsActive
               && date >= EffectiveFrom
               && (EffectiveTo == null || date <= EffectiveTo);
    }

    /// <summary>
    ///     Check if rate applies to amount
    /// </summary>
    public bool AppliesToAmount(decimal amount)
    {
        if (MinimumTaxableAmount.HasValue && amount < MinimumTaxableAmount.Value)
            return false;

        if (MaximumTaxableAmount.HasValue && amount > MaximumTaxableAmount.Value)
            return false;

        return true;
    }
}

/// <summary>
///     Tax type enumeration
/// </summary>
public enum TaxType
{
    VAT = 1,              // Value Added Tax (EU)
    GST = 2,              // Goods and Services Tax
    SalesTax = 3,         // US Sales Tax
    ServiceTax = 4,       // Service Tax
    WithholdingTax = 5,   // Withholding Tax
    ExciseTax = 6,        // Excise Tax
    CustomsDuty = 7,      // Customs and Import Duty
    Other = 99
}
