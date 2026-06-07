namespace GameGuild.Commerce.Payments;

/// <summary>Tax rule types</summary>
public enum TaxRuleType
{
    /// <summary>Standard rate</summary>
    Standard = 0,

    /// <summary>Reduced rate</summary>
    Reduced = 1,

    /// <summary>Zero rated</summary>
    ZeroRated = 2,

    /// <summary>Exempt</summary>
    Exempt = 3,

    /// <summary>Reverse charge</summary>
    ReverseCharge = 4,

    /// <summary>Withholding tax</summary>
    WithholdingTax = 5,

    /// <summary>Compound tax</summary>
    Compound = 6,

    /// <summary>Custom</summary>
    Custom = 7
}
