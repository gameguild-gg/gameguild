using GameGuild.CQRS;
using GameGuild.Modules.Products.Domain.Enums;
// using GameGuild.Modules.Contents.Domain.Entities;


namespace GameGuild.Modules.Products.Application.Features.ManageProduct;

/// <summary> Command to create a new product </summary>
public record CreateProductCommand : IRequest<CreateProductResult>
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ShortDescription { get; init; }

    public string? ImageUrl { get; init; }

    public ProductType Type { get; init; } = ProductType.Program;

    public bool IsBundle { get; init; }

    public Guid CreatorId { get; init; }

    public List<Guid>? BundleItems { get; init; }

    public decimal ReferralCommissionPercentage { get; init; } = 30m;

    public decimal MaxAffiliateDiscount { get; init; }

    public decimal AffiliateCommissionPercentage { get; init; } = 30m;

    public AccessLevel Visibility { get; init; } = AccessLevel.Public;

    public ContentStatus Status { get; init; } = ContentStatus.Draft;

    public Guid? TenantId { get; init; }
}

/// <summary> Command to update an existing product </summary>
public record UpdateProductCommand : IRequest<UpdateProductResult>
{
    public Guid ProductId { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? ShortDescription { get; init; }

    public string? ImageUrl { get; init; }

    public ProductType? Type { get; init; }

    public bool? IsBundle { get; init; }

    public List<Guid>? BundleItems { get; init; }

    public decimal? ReferralCommissionPercentage { get; init; }

    public decimal? MaxAffiliateDiscount { get; init; }

    public decimal? AffiliateCommissionPercentage { get; init; }

    public AccessLevel? Visibility { get; init; }

    public ContentStatus? Status { get; init; }

    public long? ExpectedVersion { get; init; }
}

/// <summary> Command to delete a product </summary>
public record DeleteProductCommand : IRequest<DeleteProductResult>
{
    public Guid ProductId { get; init; }

    public bool SoftDelete { get; init; } = true;

    public string? Reason { get; init; }

    public long? ExpectedVersion { get; init; }
}

/// <summary> Command to restore a soft deleted product </summary>
public record RestoreProductCommand : IRequest<RestoreProductResult>
{
    public Guid ProductId { get; init; }

    public string? Reason { get; init; }

    public long? ExpectedVersion { get; init; }
}

/// <summary> Command to add a promo code </summary>
public record CreatePromoCodeCommand : IRequest<CreatePromoCodeResult>
{
    public string Code { get; init; } = string.Empty;

    public string? Description { get; init; }

    public PromoCodeType Type { get; init; } = PromoCodeType.PercentageOff;

    public decimal? DiscountPercentage { get; init; }

    public decimal? DiscountAmount { get; init; }

    public string Currency { get; init; } = "USD";

    public DateTime? StartDate { get; init; }

    public DateTime? ExpiryDate { get; init; }

    public int? MaxUses { get; init; }

    public int? MaxUsesPerUser { get; init; } = 1;

    public bool IsActive { get; init; } = true;

    public List<Guid>? ProductIds { get; init; }

    public Guid? TenantId { get; init; }
}

/// <summary> Command to update a promo code </summary>
public record UpdatePromoCodeCommand : IRequest<UpdatePromoCodeResult>
{
    public Guid PromoCodeId { get; init; }

    public string? Code { get; init; }

    public string? Description { get; init; }

    public PromoCodeType? Type { get; init; }

    public decimal? DiscountPercentage { get; init; }

    public decimal? DiscountAmount { get; init; }

    public string? Currency { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? ExpiryDate { get; init; }

    public int? MaxUses { get; init; }

    public int? MaxUsesPerUser { get; init; }

    public bool? IsActive { get; init; }

    public List<Guid>? ProductIds { get; init; }

    public long? ExpectedVersion { get; init; }
}

/// <summary> Command to set product pricing </summary>
public record SetProductPricingCommand : IRequest<SetProductPricingResult>
{
    public Guid ProductId { get; init; }

    public decimal BasePrice { get; init; }

    public decimal? SalePrice { get; init; }

    public string Currency { get; init; } = "USD";

    public DateTime? SaleStartDate { get; init; }

    public DateTime? SaleEndDate { get; init; }

    public bool IsDefault { get; init; } = true;

    public long? ExpectedVersion { get; init; }
}

/// <summary> Command to apply promo code to user </summary>
public record ApplyPromoCodeCommand : IRequest<ApplyPromoCodeResult>
{
    public string Code { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public Guid ProductId { get; init; }

    public decimal OriginalPrice { get; init; }

    public string Currency { get; init; } = "USD";

    public Guid? TenantId { get; init; }
}

// Result types
public record CreateProductResult(bool Success, Guid? ProductId = null, string? Error = null);
public record UpdateProductResult(bool Success, string? Error = null);
public record DeleteProductResult(bool Success, string? Error = null);
public record RestoreProductResult(bool Success, string? Error = null);
public record CreatePromoCodeResult(bool Success, Guid? PromoCodeId = null, string? Error = null);
public record UpdatePromoCodeResult(bool Success, string? Error = null);
public record SetProductPricingResult(bool Success, string? Error = null);
public record ApplyPromoCodeResult(bool Success, decimal? DiscountAmount = null, decimal? FinalPrice = null, string? Error = null);
