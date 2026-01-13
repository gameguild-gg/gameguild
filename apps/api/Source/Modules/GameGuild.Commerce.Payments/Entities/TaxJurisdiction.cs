using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing a tax jurisdiction</summary>
[Table("tax_jurisdictions")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Type))]
[Index(nameof(ParentJurisdictionId))]
[Index(nameof(IsActive))]
public abstract class TaxJurisdiction : EntityBase
{
    /// <summary>Default constructor</summary>
    public TaxJurisdiction() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial jurisdiction data</param>
    public TaxJurisdiction(object partial) : base(partial) { }

    /// <summary>Jurisdiction code</summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Jurisdiction name</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Jurisdiction type</summary>
    public TaxJurisdictionType Type { get; set; }

    /// <summary>Parent jurisdiction ID</summary>
    public Guid? ParentJurisdictionId { get; set; }

    /// <summary>Navigation property to parent jurisdiction</summary>
    [ForeignKey(nameof(ParentJurisdictionId))]
    public virtual TaxJurisdiction? ParentJurisdiction { get; set; }

    /// <summary>Navigation property to child jurisdictions</summary>
    [InverseProperty(nameof(ParentJurisdiction))]
    public virtual ICollection<TaxJurisdiction> ChildJurisdictions { get; } = new List<TaxJurisdiction>();

    /// <summary>Whether this jurisdiction is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Tax registration number</summary>
    [MaxLength(100)]
    public string? TaxRegistrationNumber { get; set; }

    /// <summary>Whether reverse charge is applicable</summary>
    public bool IsReverseChargeApplicable { get; set; }

    /// <summary>Navigation property to tax rules</summary>
    public virtual ICollection<TaxRule> TaxRules { get; } = new List<TaxRule>();
}
