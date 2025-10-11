using GameGuild.CQRS;


namespace GameGuild.Modules.Products.Application.Features.ManagePricing;

/// <summary>Command to create a pricing rule</summary>
public record CreatePricingRuleCommand(
    Guid ProductId,
    string Name,
    string? Description,
    int RuleType, // PricingRuleType
    int Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    int? MinQuantity,
    int? MaxQuantity,
    decimal? DiscountPercentage,
    decimal? FixedPrice,
    string? Region,
    string? CustomerSegment
) : IRequest<Guid>;
