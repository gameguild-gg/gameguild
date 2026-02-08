namespace GameGuild.Commerce.Products;

/// <summary>
/// Product Data Transfer Object for API responses
/// </summary>
/// <param name="Id">Product ID</param>
/// <param name="Name">Product name</param>
/// <param name="Description">Product description</param>
/// <param name="ShortDescription">Short description</param>
/// <param name="ImageUrl">Image URL</param>
/// <param name="Type">Product type</param>
/// <param name="IsBundle">Whether product is a bundle</param>
/// <param name="CreatorId">Creator user ID</param>
/// <param name="BundleItems">Bundle item IDs</param>
/// <param name="ReferralCommissionPercentage">Referral commission percentage</param>
/// <param name="MaxAffiliateDiscount">Max affiliate discount</param>
/// <param name="AffiliateCommissionPercentage">Affiliate commission percentage</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
/// <param name="Pricing">Pricing information (optional)</param>
public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? ShortDescription,
    string? ImageUrl,
    ProductType Type,
    bool IsBundle,
    Guid? CreatorId,
    List<Guid>? BundleItems,
    decimal ReferralCommissionPercentage,
    decimal MaxAffiliateDiscount,
    decimal AffiliateCommissionPercentage,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ProductPricingDto>? Pricing = null
);

/// <summary>
/// Product Pricing Data Transfer Object
/// </summary>
/// <param name="Id">Pricing ID</param>
/// <param name="ProductId">Associated product ID</param>
/// <param name="Name">Pricing option name</param>
/// <param name="BasePrice">Base price</param>
/// <param name="SalePrice">Sale price (if applicable)</param>
/// <param name="Currency">Currency code</param>
/// <param name="SaleStartDate">Sale start date</param>
/// <param name="SaleEndDate">Sale end date</param>
/// <param name="IsDefault">Whether this is the default pricing</param>
/// <param name="CurrentPrice">Current effective price</param>
/// <param name="IsSaleActive">Whether sale is currently active</param>
public record ProductPricingDto(
    Guid Id,
    Guid ProductId,
    string Name,
    decimal BasePrice,
    decimal? SalePrice,
    string Currency,
    DateTime? SaleStartDate,
    DateTime? SaleEndDate,
    bool IsDefault,
    decimal CurrentPrice,
    bool IsSaleActive
);
