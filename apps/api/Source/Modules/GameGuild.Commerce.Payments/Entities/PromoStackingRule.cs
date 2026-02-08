using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing promo code stacking rules</summary>
[Table("promo_stacking_rules")]
[Index(nameof(PromoCodeId))]
[Index(nameof(StackBehavior))]
[Index(nameof(Priority))]
public class PromoStackingRule : EntityBase
{
    /// <summary>Default constructor</summary>
    public PromoStackingRule() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial rule data</param>
    public PromoStackingRule(object partial) : base(partial) { }

    /// <summary>Foreign key to the PromoCode entity</summary>
    [Required]
    public Guid PromoCodeId { get; set; }

    /// <summary>Stack behavior</summary>
    public StackBehavior StackBehavior { get; set; }

    /// <summary>Allowed promo code IDs (JSON array)</summary>
    [MaxLength(2000)]
    public string? AllowedPromoCodeIds { get; set; }

    /// <summary>Excluded promo code IDs (JSON array)</summary>
    [MaxLength(2000)]
    public string? ExcludedPromoCodeIds { get; set; }

    /// <summary>Maximum number of stackable promos</summary>
    public int? MaxStackablePromosCount { get; set; }

    /// <summary>Promo code types (JSON array)</summary>
    [MaxLength(1000)]
    public string? PromoCodeTypes { get; set; }

    /// <summary>Rule priority</summary>
    public int Priority { get; set; }

    /// <summary>Rule description</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Get allowed promo code IDs</summary>
    public List<Guid> GetAllowedPromoCodeIds()
    {
        if (string.IsNullOrWhiteSpace(AllowedPromoCodeIds)) return new List<Guid>();

        try { return JsonSerializer.Deserialize<List<Guid>>(AllowedPromoCodeIds) ?? new List<Guid>(); }
        catch { return new List<Guid>(); }
    }

    /// <summary>Set allowed promo code IDs</summary>
    public void SetAllowedPromoCodeIds(List<Guid> ids) { AllowedPromoCodeIds = ids.Count > 0 ? JsonSerializer.Serialize(ids) : null; }

    /// <summary>Get excluded promo code IDs</summary>
    public List<Guid> GetExcludedPromoCodeIds()
    {
        if (string.IsNullOrWhiteSpace(ExcludedPromoCodeIds)) return new List<Guid>();

        try { return JsonSerializer.Deserialize<List<Guid>>(ExcludedPromoCodeIds) ?? new List<Guid>(); }
        catch { return new List<Guid>(); }
    }

    /// <summary>Set excluded promo code IDs</summary>
    public void SetExcludedPromoCodeIds(List<Guid> ids) { ExcludedPromoCodeIds = ids.Count > 0 ? JsonSerializer.Serialize(ids) : null; }

    /// <summary>Get promo code types</summary>
    public List<string> GetPromoCodeTypes()
    {
        if (string.IsNullOrWhiteSpace(PromoCodeTypes)) return new List<string>();

        try { return JsonSerializer.Deserialize<List<string>>(PromoCodeTypes) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    /// <summary>Set promo code types</summary>
    public void SetPromoCodeTypes(List<string> types) { PromoCodeTypes = types.Count > 0 ? JsonSerializer.Serialize(types) : null; }
}
