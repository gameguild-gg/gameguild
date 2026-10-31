using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Localized price point for a subscription plan in one tenant-enabled currency (issue
/// #412 spec C). One row per (tenant, plan, currency, effective date): either an
/// auto-calculated price (recomputed from the FX seam on every recalculation run, recording
/// the rate that produced it) or a manual override (psychological pricing such as €8.99
/// instead of €8.67 — never touched by recalculation). Effective/expiry dating supports
/// future price changes and temporary overrides: the applicable row at instant T is the one
/// with the newest effective date at or before T whose expiry (when set) is still in the
/// future. PCI DSS note: currency codes, amounts and rates are not cardholder data; this
/// table stores no payment credentials and never co-locates with PAN data.
/// </summary>
[Table("price_localizations")]
[Index(nameof(TenantId))]
[Index(nameof(PlanId))]
[Index(nameof(CurrencyCode))]
[Index(nameof(EffectiveDateUtc))]
[Index(nameof(IsAutoCalculated))]
[Index(nameof(TenantId), nameof(PlanId), nameof(CurrencyCode), nameof(EffectiveDateUtc), IsUnique = true)]
public class PriceLocalization : EntityBase
{
    /// <summary>Default constructor</summary>
    public PriceLocalization() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial localization data</param>
    public PriceLocalization(object partial) : base(partial) { }

    /// <summary>
    ///     Tenant the localization belongs to (inherited tenant scope); required — localized
    ///     prices are per-tenant market decisions.
    /// </summary>
    [Required]
    public override Guid? TenantId { get; set; }

    /// <summary>Plan the localized price applies to (plan IDs are globally unique).</summary>
    public Guid PlanId { get; set; }

    /// <summary>ISO 4217 code the price is denominated in; always upper-case, tenant-enabled at write time.</summary>
    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    ///     Localized monthly price. Manual rows carry the operator's exact value; auto rows
    ///     carry the last FX-calculated value (see <see cref="ExchangeRateUsed"/>).
    /// </summary>
    [Column(TypeName = "decimal(28,12)")]
    public decimal LocalizedPrice { get; set; }

    /// <summary>
    ///     True when the price is FX-derived and refreshable by recalculation runs; false for
    ///     manual overrides, which recalculation always preserves.
    /// </summary>
    public bool IsAutoCalculated { get; set; }

    /// <summary>Moment (UTC) of the last automatic calculation; null for manual overrides.</summary>
    public DateTime? LastCalculatedAt { get; set; }

    /// <summary>Quotation (target units per one plan-currency unit) that produced the last auto calculation.</summary>
    [Column(TypeName = "decimal(28,12)")]
    public decimal? ExchangeRateUsed { get; set; }

    /// <summary>First instant (UTC) the row applies to; supports scheduling future price changes.</summary>
    public DateTime EffectiveDateUtc { get; set; }

    /// <summary>Optional last instant (UTC) the row applies to; temporary overrides expire and stop applying.</summary>
    public DateTime? ExpiryDateUtc { get; set; }
}

/// <summary>Outcome values for a localized-price recalculation run.</summary>
public enum PriceRecalculationRunStatus
{
    /// <summary>Run is executing (progress fields update as batches complete).</summary>
    Running = 0,

    /// <summary>Run finished; auto prices refreshed, overrides preserved.</summary>
    Succeeded = 1,

    /// <summary>Run aborted by an error; partial results remain until rolled back or re-run.</summary>
    Failed = 2,

    /// <summary>Run results were reverted through the rollback command.</summary>
    RolledBack = 3
}
