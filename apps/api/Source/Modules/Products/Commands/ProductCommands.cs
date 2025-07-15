using MediatR;
using GameGuild.Modules.Products.Models;
using GameGuild.Modules.Contents;
using GameGuild.Common;

namespace GameGuild.Modules.Products.Commands;

/// <summary>
/// Command to create a new product
/// </summary>
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

/// <summary>
/// Command to update an existing product
/// </summary>
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
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to delete a product
/// </summary>
public record DeleteProductCommand : IRequest<DeleteProductResult>
{
    public Guid ProductId { get; init; }
    public Guid DeletedBy { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Command to add pricing to a product
/// </summary>
public record AddProductPricingCommand : IRequest<AddProductPricingResult>
{
    public Guid ProductId { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string? PricingTier { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsActive { get; init; } = true;
    public Guid CreatedBy { get; init; }
}

/// <summary>
/// Command to grant user access to a product
/// </summary>
public record GrantUserProductAccessCommand : IRequest<GrantUserProductAccessResult>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public ProductAcquisitionType AcquisitionType { get; init; }
    public decimal PurchasePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime? ExpiresAt { get; init; }
    public Guid GrantedBy { get; init; }
}

/// <summary>
/// Command to revoke user access to a product
/// </summary>
public record RevokeUserProductAccessCommand : IRequest<RevokeUserProductAccessResult>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public string? Reason { get; init; }
    public Guid RevokedBy { get; init; }
}

/// <summary>
/// Command to publish a product
/// </summary>
public record PublishProductCommand : IRequest<PublishProductResult>
{
    public Guid ProductId { get; init; }
    public Guid PublishedBy { get; init; }
}

/// <summary>
/// Command to unpublish a product
/// </summary>
public record UnpublishProductCommand : IRequest<UnpublishProductResult>
{
    public Guid ProductId { get; init; }
    public Guid UnpublishedBy { get; init; }
}

/// <summary>
/// Command to archive a product
/// </summary>
public record ArchiveProductCommand : IRequest<ArchiveProductResult>
{
    public Guid ProductId { get; init; }
    public Guid ArchivedBy { get; init; }
}

/// <summary>
/// Command to set product visibility
/// </summary>
public record SetProductVisibilityCommand : IRequest<SetProductVisibilityResult>
{
    public Guid ProductId { get; init; }
    public AccessLevel Visibility { get; init; }
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to add product to bundle
/// </summary>
public record AddToBundleCommand : IRequest<AddToBundleResult>
{
    public Guid BundleId { get; init; }
    public Guid ProductId { get; init; }
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to remove product from bundle
/// </summary>
public record RemoveFromBundleCommand : IRequest<RemoveFromBundleResult>
{
    public Guid BundleId { get; init; }
    public Guid ProductId { get; init; }
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to set product pricing
/// </summary>
public record SetPricingCommand : IRequest<SetPricingResult>
{
    public Guid ProductId { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string? PricingTier { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to create subscription plan
/// </summary>
public record CreateSubscriptionPlanCommand : IRequest<CreateSubscriptionPlanResult>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string Interval { get; init; } = "month"; // month, year
    public int IntervalCount { get; init; } = 1;
    public int? TrialDays { get; init; }
    public bool IsActive { get; init; } = true;
    public Guid CreatedBy { get; init; }
}

/// <summary>
/// Command to grant user access to product
/// </summary>
public record GrantUserAccessCommand : IRequest<GrantUserAccessResult>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public ProductAcquisitionType AcquisitionType { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public Guid GrantedBy { get; init; }
}

/// <summary>
/// Command to revoke user access to product
/// </summary>
public record RevokeUserAccessCommand : IRequest<RevokeUserAccessResult>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public string? Reason { get; init; }
    public Guid RevokedBy { get; init; }
}

/// <summary>
/// Result types for product commands
/// </summary>
public record CreateProductResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record UpdateProductResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record DeleteProductResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public record AddProductPricingResult
{
    public bool Success { get; init; }
    public ProductPricing? Pricing { get; init; }
    public string? ErrorMessage { get; init; }
}

public record GrantUserProductAccessResult
{
    public bool Success { get; init; }
    public UserProduct? UserProduct { get; init; }
    public string? ErrorMessage { get; init; }
}

public record RevokeUserProductAccessResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public record PublishProductResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record UnpublishProductResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record ArchiveProductResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record SetProductVisibilityResult
{
    public bool Success { get; init; }
    public Product? Product { get; init; }
    public string? ErrorMessage { get; init; }
}

public record AddToBundleResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public record RemoveFromBundleResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public record SetPricingResult
{
    public bool Success { get; init; }
    public ProductPricing? Pricing { get; init; }
    public string? ErrorMessage { get; init; }
}

public record CreateSubscriptionPlanResult
{
    public bool Success { get; init; }
    public ProductSubscriptionPlan? Plan { get; init; }
    public string? ErrorMessage { get; init; }
}

public record GrantUserAccessResult
{
    public bool Success { get; init; }
    public UserProduct? UserProduct { get; init; }
    public string? ErrorMessage { get; init; }
}

public record RevokeUserAccessResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
