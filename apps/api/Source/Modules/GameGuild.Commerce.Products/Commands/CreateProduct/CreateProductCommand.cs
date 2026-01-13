using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to create a new product
/// </summary>
/// <param name="Name">Product name</param>
/// <param name="Description">Product description</param>
/// <param name="ShortDescription">Short description for listings</param>
/// <param name="ImageUrl">Product image URL</param>
/// <param name="Type">Type of product</param>
/// <param name="IsBundle">Whether this is a bundle of products</param>
/// <param name="CreatorId">ID of the user creating the product</param>
/// <param name="BundleItems">List of product IDs if this is a bundle</param>
/// <param name="ReferralCommissionPercentage">Referral commission percentage</param>
/// <param name="MaxAffiliateDiscount">Maximum affiliate discount</param>
/// <param name="AffiliateCommissionPercentage">Affiliate commission percentage</param>
/// <param name="TenantId">Optional tenant ID</param>
public record CreateProductCommand(
    string Name,
    string? Description = null,
    string? ShortDescription = null,
    string? ImageUrl = null,
    ProductType Type = ProductType.Program,
    bool IsBundle = false,
    Guid? CreatorId = null,
    List<Guid>? BundleItems = null,
    decimal ReferralCommissionPercentage = 30m,
    decimal MaxAffiliateDiscount = 0m,
    decimal AffiliateCommissionPercentage = 30m,
    Guid? TenantId = null
) : ICommand<ProductDto>;
