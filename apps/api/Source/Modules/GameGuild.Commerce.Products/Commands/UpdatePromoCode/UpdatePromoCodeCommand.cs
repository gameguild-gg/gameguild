using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to update an existing promo code
/// </summary>
/// <param name="Id">Promo code ID</param>
/// <param name="Name">Display name</param>
/// <param name="Description">Optional description</param>
/// <param name="Type">Type of discount</param>
/// <param name="DiscountPercentage">Percentage discount</param>
/// <param name="DiscountAmount">Fixed amount discount</param>
/// <param name="Currency">Currency code</param>
/// <param name="MinimumOrderAmount">Minimum order amount</param>
/// <param name="MaxUses">Maximum total uses</param>
/// <param name="MaxUsesPerUser">Maximum uses per user</param>
/// <param name="ValidFrom">Start date</param>
/// <param name="ValidUntil">End date</param>
/// <param name="IsActive">Whether the code is active</param>
/// <param name="IsExclusive">Whether the code cannot be stacked</param>
/// <param name="StackingPriority">Priority for stacking</param>
/// <param name="ProductId">Specific product ID</param>
public sealed record UpdatePromoCodeCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    PromoCodeType? Type = null,
    decimal? DiscountPercentage = null,
    decimal? DiscountAmount = null,
    string? Currency = null,
    decimal? MinimumOrderAmount = null,
    int? MaxUses = null,
    int? MaxUsesPerUser = null,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null,
    bool? IsActive = null,
    bool? IsExclusive = null,
    int? StackingPriority = null,
    Guid? ProductId = null
) : ICommand<PromoCodeDto>;
