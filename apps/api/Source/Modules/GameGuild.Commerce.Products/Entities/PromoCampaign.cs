using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Identity.Users;

namespace GameGuild.Commerce.Products;

/// <summary>Lifecycle status of a promotional campaign (issue #418).</summary>
public enum PromoCampaignStatus
{
    /// <summary>Being configured; codes may reference the key but are not expected to run yet.</summary>
    Draft = 0,

    /// <summary>Live: codes under the campaign key may validate and redeem.</summary>
    Active = 1,

    /// <summary>Temporarily halted (operational hold); codes keep their own windows.</summary>
    Paused = 2,

    /// <summary>Finished: window closed, reporting stays available.</summary>
    Completed = 3,

    /// <summary>Withdrawn early; kept for reporting history.</summary>
    Cancelled = 4,

    /// <summary>Soft-archived after completion/cancellation; hidden from active lists.</summary>
    Archived = 5
}

/// <summary>
/// A promotional campaign: the managed entity behind the free-form
/// <see cref="PromoCode.CampaignId"/> grouping key (issue #418). Carries the campaign
/// lifecycle (draft → active → paused/completed/cancelled → archived), the spend inputs
/// used for customer-acquisition-cost attribution, and its reporting window. Promo codes
/// reference the campaign by its tenant-unique <see cref="Key"/> — plain string join,
/// no FK, mirroring the plan-scoping pattern.
/// </summary>
[Table("promo_campaigns")]
public class PromoCampaign : EntityBase
{
    /// <summary>Default constructor</summary>
    public PromoCampaign() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial campaign data</param>
    public PromoCampaign(object partial) : base(partial) { }

    /// <summary>Campaign display name</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The campaign grouping key promo codes reference via <c>CampaignId</c>.
    /// Unique per tenant, immutable after creation.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>What the campaign is about</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Lifecycle status</summary>
    public PromoCampaignStatus Status { get; private set; } = PromoCampaignStatus.Draft;

    /// <summary>When the campaign window opens (UTC instant)</summary>
    public DateTime? StartsAtUtc { get; set; }

    /// <summary>When the campaign window closes (UTC instant)</summary>
    public DateTime? EndsAtUtc { get; set; }

    /// <summary>IANA timezone the campaign window is expressed in (default UTC)</summary>
    [MaxLength(50)]
    public string? TimeZoneId { get; set; }

    /// <summary>Total planned spend for the campaign (discount budget, media, ...)</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal? Budget { get; set; }

    /// <summary>Acquisition spend attributed to the campaign; the numerator of CAC.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal? AcquisitionSpend { get; set; }

    /// <summary>Currency of the spend fields (ISO 4217)</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Foreign key to the user who created this campaign</summary>
    [Required]
    public Guid CreatedBy { get; set; }

    /// <summary>Navigation property to the creating user</summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>Whether the campaign window is currently open (UTC-instant evaluation).</summary>
    public bool IsWindowOpen(DateTime? utcNow = null)
    {
        var now = (utcNow ?? SystemClock.UtcNow).ToUniversalTime();
        if (StartsAtUtc.HasValue && now < StartsAtUtc.Value.ToUniversalTime())
        {
            return false;
        }

        if (EndsAtUtc.HasValue && now >= EndsAtUtc.Value.ToUniversalTime())
        {
            return false;
        }

        return true;
    }

    /// <summary>Activates a draft (or re-activates a paused) campaign.</summary>
    public void Activate()
    {
        RequireStatus("activate", PromoCampaignStatus.Draft, PromoCampaignStatus.Paused);
        Status = PromoCampaignStatus.Active;
        Touch();
    }

    /// <summary>Pauses an active campaign (operational hold).</summary>
    public void Pause()
    {
        RequireStatus("pause", PromoCampaignStatus.Active);
        Status = PromoCampaignStatus.Paused;
        Touch();
    }

    /// <summary>Completes an active or paused campaign (window closed).</summary>
    public void Complete()
    {
        RequireStatus("complete", PromoCampaignStatus.Active, PromoCampaignStatus.Paused);
        Status = PromoCampaignStatus.Completed;
        Touch();
    }

    /// <summary>Cancels a not-yet-completed campaign.</summary>
    public void Cancel()
    {
        RequireStatus("cancel", PromoCampaignStatus.Draft, PromoCampaignStatus.Active, PromoCampaignStatus.Paused);
        Status = PromoCampaignStatus.Cancelled;
        Touch();
    }

    /// <summary>Archives a completed or cancelled campaign.</summary>
    public void Archive()
    {
        RequireStatus("archive", PromoCampaignStatus.Completed, PromoCampaignStatus.Cancelled);
        Status = PromoCampaignStatus.Archived;
        Touch();
    }

    private void RequireStatus(string operation, params PromoCampaignStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException(
                $"Cannot {operation} promo campaign in status {Status}; allowed from: {string.Join(", ", allowed)}.");
        }
    }
}
