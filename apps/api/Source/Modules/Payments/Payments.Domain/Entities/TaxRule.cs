using GameGuild.Modules.Common;

namespace GameGuild.Modules.Payments.Domain.Entities;

/// <summary>
///     Represents a tax calculation rule
/// </summary>
public class TaxRule : EntityBase
{
    /// <summary>
    ///     Rule name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Rule description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Tax jurisdiction ID
    /// </summary>
    public Guid TaxJurisdictionId { get; set; }

    /// <summary>
    ///     Tax jurisdiction navigation
    /// </summary>
    public TaxJurisdiction TaxJurisdiction { get; set; } = null!;

    /// <summary>
    ///     Rule type
    /// </summary>
    public TaxRuleType RuleType { get; set; }

    /// <summary>
    ///     Priority (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Is rule active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Effective start date
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    ///     Effective end date (null = indefinite)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    ///     Customer type filter (B2B, B2C, null = all)
    /// </summary>
    public CustomerType? CustomerTypeFilter { get; set; }

    /// <summary>
    ///     Product categories this rule applies to (JSON array)
    /// </summary>
    public string? ProductCategories { get; set; }

    /// <summary>
    ///     Minimum transaction amount
    /// </summary>
    public decimal? MinimumAmount { get; set; }

    /// <summary>
    ///     Maximum transaction amount
    /// </summary>
    public decimal? MaximumAmount { get; set; }

    /// <summary>
    ///     Is tax included in price
    /// </summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>
    ///     Is reverse charge applicable
    /// </summary>
    public bool IsReverseCharge { get; set; }

    /// <summary>
    ///     Tax exemption conditions (JSON)
    /// </summary>
    public string? ExemptionConditions { get; set; }

    /// <summary>
    ///     Default tax rate ID
    /// </summary>
    public Guid? DefaultTaxRateId { get; set; }

    /// <summary>
    ///     Default tax rate navigation
    /// </summary>
    public TaxRate? DefaultTaxRate { get; set; }

    /// <summary>
    ///     Check if rule is currently effective
    /// </summary>
    public bool IsEffective(DateTime date)
    {
        return IsActive
               && date >= EffectiveFrom
               && (EffectiveTo == null || date <= EffectiveTo);
    }

    /// <summary>
    ///     Check if rule applies to transaction
    /// </summary>
    public bool AppliesToTransaction(decimal amount, CustomerType customerType)
    {
        if (!IsActive)
            return false;

        if (CustomerTypeFilter.HasValue && customerType != CustomerTypeFilter.Value)
            return false;

        if (MinimumAmount.HasValue && amount < MinimumAmount.Value)
            return false;

        if (MaximumAmount.HasValue && amount > MaximumAmount.Value)
            return false;

        return true;
    }
}

/// <summary>
///     Tax rule type
/// </summary>
public enum TaxRuleType
{
    Standard = 1,         // Standard tax calculation
    Reduced = 2,          // Reduced rate
    ZeroRated = 3,        // Zero rate (but VAT registered)
    Exempt = 4,           // Tax exempt
    ReverseCharge = 5,    // Reverse charge (B2B EU)
    WithholdingTax = 6,   // Withholding tax
    Compound = 7,         // Compound tax (tax on tax)
    Custom = 99
}

/// <summary>
///     Customer type for tax purposes
/// </summary>
public enum CustomerType
{
    B2C = 1,  // Business to Consumer
    B2B = 2   // Business to Business
}
