using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Identity.Users;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Customer-acquisition-cost attribution row (issue #418): links a customer to the
/// campaign whose code tagged their first purchase. One row per customer — the
/// unique index on <see cref="UserId"/> makes the first attribution atomic, so a
/// later redemption under a different campaign can never re-attribute the customer.
/// CAC is computed as the campaign's acquisition spend divided by its attributed
/// customers.
/// </summary>
[Table("promo_campaign_attributions")]
public class PromoCampaignAttribution : EntityBase
{
    /// <summary>Default constructor</summary>
    public PromoCampaignAttribution() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial attribution data</param>
    public PromoCampaignAttribution(object partial) : base(partial) { }

    /// <summary>The acquired customer</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the acquired customer</summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// The campaign grouping key (<c>PromoCode.CampaignId</c>) that tagged the first
    /// purchase — the campaign source tag of the acquisition.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CampaignKey { get; set; } = string.Empty;

    /// <summary>The promo code whose redemption tagged the first purchase</summary>
    [MaxLength(50)]
    public string? Code { get; set; }

    /// <summary>The promo usage row that produced the attribution</summary>
    public Guid? FirstPurchaseUsageId { get; set; }

    /// <summary>Optional order the first purchase settled on</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Optional subscription the first purchase created</summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>When the attribution was recorded (UTC)</summary>
    public DateTime AttributedAtUtc { get; set; } = SystemClock.UtcNow;
}
