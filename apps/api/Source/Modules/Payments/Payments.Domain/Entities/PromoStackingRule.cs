using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Core.Domain;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Defines stacking rules for promo codes</summary>
public class PromoStackingRule : EntityBase
{
    /// <summary>Promo code this rule applies to</summary>
    [Required]
    public Guid PromoCodeId { get; set; }

    /// <summary>Stacking behavior</summary>
    [Required]
    public StackBehavior StackBehavior { get; set; }

    /// <summary>Allowed promo code IDs (JSON)</summary>
    [MaxLength(2000)]
    public string? AllowedPromoCodeIds { get; set; }

    /// <summary>Excluded promo code IDs (JSON)</summary>
    [MaxLength(2000)]
    public string? ExcludedPromoCodeIds { get; set; }

    /// <summary>Maximum number of stackable promos</summary>
    public int? MaxStackablePromosCount { get; set; }

    /// <summary>Allowed promo code types (JSON)</summary>
    [MaxLength(500)]
    public string? PromoCodeTypes { get; set; }

    /// <summary>Rule priority</summary>
    public int Priority { get; set; }

    /// <summary>Rule description</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Get allowed promo code IDs</summary>
    public List<Guid> GetAllowedPromoCodeIds()
    {
        if (string.IsNullOrWhiteSpace(AllowedPromoCodeIds))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(AllowedPromoCodeIds) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>Set allowed promo code IDs</summary>
    public void SetAllowedPromoCodeIds(List<Guid> ids)
    {
        AllowedPromoCodeIds = JsonSerializer.Serialize(ids);
    }

    /// <summary>Get excluded promo code IDs</summary>
    public List<Guid> GetExcludedPromoCodeIds()
    {
        if (string.IsNullOrWhiteSpace(ExcludedPromoCodeIds))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(ExcludedPromoCodeIds) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>Set excluded promo code IDs</summary>
    public void SetExcludedPromoCodeIds(List<Guid> ids)
    {
        ExcludedPromoCodeIds = JsonSerializer.Serialize(ids);
    }

    /// <summary>Get promo code types</summary>
    public List<string> GetPromoCodeTypes()
    {
        if (string.IsNullOrWhiteSpace(PromoCodeTypes))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(PromoCodeTypes) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Set promo code types</summary>
    public void SetPromoCodeTypes(List<string> types)
    {
        PromoCodeTypes = JsonSerializer.Serialize(types);
    }
}

/// <summary>Stack behavior for promo codes</summary>
public enum StackBehavior
{
    /// <summary>Allow stacking with any promo</summary>
    Allow = 1,
    /// <summary>Deny stacking completely</summary>
    Deny = 2,
    /// <summary>Allow only if this promo is first</summary>
    AllowIfFirst = 3,
    /// <summary>Allow only if this promo is last</summary>
    AllowIfLast = 4,
    /// <summary>Allow only with specific promos</summary>
    OnlyWithSpecific = 5,
    /// <summary>Maximum one per type</summary>
    MaxOnePerType = 6
}
