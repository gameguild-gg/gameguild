using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing a tax rate</summary>
[Table("tax_rates")]
[Index(nameof(TaxJurisdictionId))]
[Index(nameof(TaxType))]
[Index(nameof(IsActive))]
[Index(nameof(EffectiveFrom))]
[Index(nameof(EffectiveTo))]
public class TaxRate : EntityBase
{
    /// <summary>Default constructor</summary>
    public TaxRate() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial rate data</param>
    public TaxRate(object partial) : base(partial) { }

    /// <summary>Foreign key to tax jurisdiction</summary>
    [Required]
    public Guid TaxJurisdictionId { get; set; }

    /// <summary>Navigation property to tax jurisdiction</summary>
    [ForeignKey(nameof(TaxJurisdictionId))]
    public virtual TaxJurisdiction TaxJurisdiction { get; set; } = null!;

    /// <summary>Tax type</summary>
    public TaxType TaxType { get; set; }

    /// <summary>Tax rate</summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal Rate { get; set; }

    /// <summary>Product category</summary>
    [MaxLength(100)]
    public string? ProductCategory { get; set; }

    /// <summary>Effective from date</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Effective to date</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Whether this rate is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Minimum taxable amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumTaxableAmount { get; set; }

    /// <summary>Maximum taxable amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumTaxableAmount { get; set; }

    /// <summary>Rate description</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Check if rate is effective on a given date</summary>
    public bool IsEffective(DateTime date)
    {
        if (!IsActive) return false;
        if (date < EffectiveFrom) return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value) return false;

        return true;
    }

    /// <summary>Check if rate applies to a given amount</summary>
    public bool AppliesToAmount(decimal amount)
    {
        if (MinimumTaxableAmount.HasValue && amount < MinimumTaxableAmount.Value) return false;

        if (MaximumTaxableAmount.HasValue && amount > MaximumTaxableAmount.Value) return false;

        return true;
    }
}
