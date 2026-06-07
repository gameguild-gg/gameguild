namespace GameGuild.Commerce.Payments;

/// <summary>Tax types</summary>
// ReSharper disable InconsistentNaming - VAT and GST are standard tax acronyms
public enum TaxType
{
    /// <summary>Value Added Tax</summary>
    VAT = 0,

    /// <summary>Goods and Services Tax</summary>
    GST = 1,

    /// <summary>Sales tax</summary>
    SalesTax = 2,

    /// <summary>Service tax</summary>
    ServiceTax = 3,

    /// <summary>Withholding tax</summary>
    WithholdingTax = 4,

    /// <summary>Excise tax</summary>
    ExciseTax = 5,

    /// <summary>Customs duty</summary>
    CustomsDuty = 6,

    /// <summary>Other</summary>
    Other = 7
}
