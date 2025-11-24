using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Payments.Entities;

/// <summary>Entity representing a tax rule</summary>
[Table("tax_rules")]
[Index(nameof(TaxJurisdictionId))]
[Index(nameof(RuleType))]
[Index(nameof(IsActive))]
[Index(nameof(Priority))]
[Index(nameof(EffectiveFrom))]
[Index(nameof(EffectiveTo))]
public abstract class TaxRule : EntityBase
{
    /// <summary>Default constructor</summary>
    public TaxRule() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial rule data</param>
    public TaxRule(object partial) : base(partial) { }

    /// <summary>Rule name</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Rule description</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Foreign key to tax jurisdiction</summary>
    [Required]
    public Guid TaxJurisdictionId { get; set; }

    /// <summary>Navigation property to tax jurisdiction</summary>
    [ForeignKey(nameof(TaxJurisdictionId))]
    public virtual TaxJurisdiction TaxJurisdiction { get; set; } = null!;

    /// <summary>Rule type</summary>
    public TaxRuleType RuleType { get; set; }

    /// <summary>Rule priority</summary>
    public int Priority { get; set; }

    /// <summary>Whether this rule is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Effective from date</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Effective to date</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Customer type filter</summary>
    public CustomerType? CustomerTypeFilter { get; set; }

    /// <summary>Product categories (JSON array)</summary>
    [MaxLength(2000)]
    public string? ProductCategories { get; set; }

    /// <summary>Minimum amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumAmount { get; set; }

    /// <summary>Maximum amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumAmount { get; set; }

    /// <summary>Whether tax is inclusive</summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>Whether reverse charge applies</summary>
    public bool IsReverseCharge { get; set; }

    /// <summary>Exemption conditions (JSON)</summary>
    [MaxLength(2000)]
    public string? ExemptionConditions { get; set; }

    /// <summary>Default tax rate ID</summary>
    public Guid? DefaultTaxRateId { get; set; }

    /// <summary>Navigation property to default tax rate</summary>
    [ForeignKey(nameof(DefaultTaxRateId))]
    public virtual TaxRate? DefaultTaxRate { get; set; }

    /// <summary>Check if rule is effective on a given date</summary>
    public bool IsEffective(DateTime date)
    {
        if (!IsActive) return false;
        if (EffectiveFrom.HasValue && date < EffectiveFrom.Value) return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value) return false;

        return true;
    }

    /// <summary>Check if rule applies to a transaction</summary>
    public bool AppliesToTransaction(decimal amount, CustomerType customerType)
    {
        if (!IsActive) return false;

        if (CustomerTypeFilter.HasValue && customerType != CustomerTypeFilter.Value) return false;

        if (MinimumAmount.HasValue && amount < MinimumAmount.Value) return false;

        if (MaximumAmount.HasValue && amount > MaximumAmount.Value) return false;

        return true;
    }
}
