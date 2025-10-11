using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Modules.Products.Domain.Enums;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.Application.DTOs;

/// <summary>
/// Product DTO for API responses
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public ProductType Type { get; init; }
    public bool IsBundle { get; init; }
    public Guid CreatorId { get; init; }
    public List<Guid>? BundleItems { get; init; }
    public decimal ReferralCommissionPercentage { get; init; }
    public decimal MaxAffiliateDiscount { get; init; }
    public decimal AffiliateCommissionPercentage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ProductPricingDto>? Pricing { get; init; }
}

/// <summary>
/// Product pricing DTO for API responses
/// </summary>
public record ProductPricingDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime? SaleStartDate { get; init; }
    public DateTime? SaleEndDate { get; init; }
    public bool IsDefault { get; init; }
    public decimal CurrentPrice => SalePrice.HasValue && IsSaleActive ? SalePrice.Value : BasePrice;
    public bool IsSaleActive => SaleStartDate.HasValue && SaleEndDate.HasValue &&
                               DateTime.UtcNow >= SaleStartDate && DateTime.UtcNow <= SaleEndDate;
}

/// <summary>
/// Promo code DTO for API responses
/// </summary>
public record PromoCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PromoCodeType Type { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime? StartDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? MaxUses { get; init; }
    public int MaxUsesPerUser { get; init; }
    public bool IsActive { get; init; }
    public int CurrentUses { get; init; }
    public bool IsCurrentlyValid { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// User product DTO for API responses
/// </summary>
public record UserProductDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public ProductAccessStatus AccessStatus { get; init; }
    public ProductAcquisitionType AcquisitionType { get; init; }
    public DateTime AcquiredAt { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public decimal? PricePaid { get; init; }
    public string? Currency { get; init; }
    public ProductDto? Product { get; init; }
}

/// <summary>
/// Create product request DTO
/// </summary>
public record CreateProductRequest
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
}

/// <summary>
/// Update product request DTO
/// </summary>
public record UpdateProductRequest
{
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
}

/// <summary>
/// Create promo code request DTO
/// </summary>
public record CreatePromoCodeRequest
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
}

/// <summary>
/// Set product pricing request DTO
/// </summary>
public record SetProductPricingRequest
{
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime? SaleStartDate { get; init; }
    public DateTime? SaleEndDate { get; init; }
    public bool IsDefault { get; init; } = true;
}

/// <summary>
/// Apply promo code request DTO
/// </summary>
public record ApplyPromoCodeRequest
{
    public string Code { get; init; } = string.Empty;
    public Guid ProductId { get; init; }
    public decimal OriginalPrice { get; init; }
    public string Currency { get; init; } = "USD";
}