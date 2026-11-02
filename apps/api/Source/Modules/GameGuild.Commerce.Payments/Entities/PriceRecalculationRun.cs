using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Audit/progress row for one localized-price recalculation pass (issue #412 spec C):
///     bulk recalculation with progress tracking and rollback. The run records the target
///     scope (all plans or one), per-item progress while it executes, aggregate counters,
///     and a before-snapshot of every auto row it rewrote so the rollback command can
///     restore the exact prior values. Tenant-scoped like the localizations it rewrites.
/// </summary>
[Table("price_recalculation_runs")]
[Index(nameof(TenantId))]
[Index(nameof(StartedAtUtc))]
[Index(nameof(Status))]
[Index(nameof(TriggerReason))]
public class PriceRecalculationRun : EntityBase
{
    /// <summary>Default constructor</summary>
    public PriceRecalculationRun() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial run data</param>
    public PriceRecalculationRun(object partial) : base(partial) { }

    /// <summary>Tenant whose localized prices were recalculated.</summary>
    [Required]
    public override Guid? TenantId { get; set; }

    /// <summary>Plan scope: null = every plan of the tenant, otherwise the single recalculated plan.</summary>
    public Guid? PlanId { get; set; }

    /// <summary>Run outcome.</summary>
    public PriceRecalculationRunStatus Status { get; set; }

    /// <summary>Number of (plan, currency) auto-price candidates the run intends to refresh.</summary>
    public int TotalItems { get; set; }

    /// <summary>Candidates processed so far (progress tracking; equals <see cref="TotalItems"/> on success).</summary>
    public int CompletedItems { get; set; }

    /// <summary>Auto rows whose stored value actually changed.</summary>
    public int UpdatedPrices { get; set; }

    /// <summary>Manual override rows encountered and deliberately left untouched.</summary>
    public int PreservedOverrides { get; set; }

    /// <summary>Auto candidates skipped because no FX quotation was available (fail closed, kept at prior value).</summary>
    public int SkippedForMissingRate { get; set; }

    /// <summary>
    ///     Before-snapshot (JSON) of every auto row rewritten: one entry per plan/currency with
    ///     the prior price, rate and timestamps. Consumed by the rollback command.
    /// </summary>
    public string BeforeSnapshot { get; set; } = "[]";

    /// <summary>Account that started the run; null when the FX sync worker triggered it.</summary>
    public Guid? TriggeredByUserId { get; set; }

    /// <summary>Why the run happened (e.g. "manual", "fx-sync").</summary>
    [MaxLength(200)]
    public string TriggerReason { get; set; } = "manual";

    /// <summary>Run start (UTC).</summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>Run completion (UTC); null while running.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Instant (UTC) the run was rolled back; null until then.</summary>
    public DateTime? RolledBackAtUtc { get; set; }

    /// <summary>Account that rolled the run back; null until then.</summary>
    public Guid? RolledBackByUserId { get; set; }
}
