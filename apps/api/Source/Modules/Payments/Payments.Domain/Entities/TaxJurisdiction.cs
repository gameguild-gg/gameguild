using GameGuild.Modules.Common;

namespace GameGuild.Modules.Payments.Domain.Entities;

/// <summary>
///     Represents a tax jurisdiction (country, state, region)
/// </summary>
public class TaxJurisdiction : EntityBase
{
    /// <summary>
    ///     Jurisdiction code (e.g., "US-CA" for California, "GB" for UK)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     Jurisdiction name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Jurisdiction type
    /// </summary>
    public TaxJurisdictionType Type { get; set; }

    /// <summary>
    ///     Parent jurisdiction ID (e.g., state's parent is country)
    /// </summary>
    public Guid? ParentJurisdictionId { get; set; }

    /// <summary>
    ///     Parent jurisdiction navigation
    /// </summary>
    public TaxJurisdiction? ParentJurisdiction { get; set; }

    /// <summary>
    ///     Child jurisdictions (e.g., states within country)
    /// </summary>
    public ICollection<TaxJurisdiction> ChildJurisdictions { get; set; } = new List<TaxJurisdiction>();

    /// <summary>
    ///     Tax rules for this jurisdiction
    /// </summary>
    public ICollection<TaxRule> TaxRules { get; set; } = new List<TaxRule>();

    /// <summary>
    ///     Is this jurisdiction active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Tax registration number in this jurisdiction
    /// </summary>
    public string? TaxRegistrationNumber { get; set; }

    /// <summary>
    ///     Is reverse charge applicable (for B2B transactions in EU)
    /// </summary>
    public bool IsReverseChargeApplicable { get; set; }
}

/// <summary>
///     Tax jurisdiction type
/// </summary>
public enum TaxJurisdictionType
{
    Country = 1,
    State = 2,
    Province = 3,
    Region = 4,
    City = 5,
    County = 6,
    District = 7
}
