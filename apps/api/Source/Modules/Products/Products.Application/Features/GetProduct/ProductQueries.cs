using GameGuild.CQRS;
using ProductEntity = GameGuild.Modules.Products.Domain.Entities.Product;
using GameGuild.Modules.Products.Domain.Enums;
// using GameGuild.Modules.Contents.Domain.Entities;


namespace GameGuild.Modules.Products.Application.Features.GetProduct;

/// <summary> Query to get product by ID </summary>
public record GetProductByIdQuery : IRequest<ProductEntity?>
{
    public Guid ProductId { get; init; }

    public bool IncludePricing { get; init; } = true;

    public bool IncludePrograms { get; init; } = true;
}

/// <summary> Query to get products list </summary>
public record GetProductsQuery : IRequest<IEnumerable<ProductEntity>>
{
    public ProductType? Type { get; init; }

    public ContentStatus? Status { get; init; }

    public AccessLevel? Visibility { get; init; }

    public Guid? CreatorId { get; init; }

    public string? SearchTerm { get; init; }

    public bool? IsBundle { get; init; }

    public int Skip { get; init; } = 0;

    public int Take { get; init; } = 50;

    public string? SortBy { get; init; } = "CreatedAt";

    public string? SortDirection { get; init; } = "DESC";
}

/// <summary> Query to get user's products (purchased/owned) </summary>
public record GetUserProductsQuery : IRequest<IEnumerable<UserProduct>>
{
    public Guid UserId { get; init; }

    public ProductAcquisitionType? AcquisitionType { get; init; }

    public bool? IsActive { get; init; }

    public int Skip { get; init; } = 0;

    public int Take { get; init; } = 50;
}

/// <summary> Query to get product pricing </summary>
public record GetProductPricingQuery : IRequest<IEnumerable<ProductPricing>>
{
    public Guid ProductId { get; init; }

    public string? Currency { get; init; }

    public bool? IsDefault { get; init; }
}

/// <summary> Query to get promo code by code </summary>
public record GetPromoCodeQuery : IRequest<PromoCode?>
{
    public string Code { get; init; } = string.Empty;

    public bool IncludeUsages { get; init; } = false;
}

/// <summary> Query to get promo codes list </summary>
public record GetPromoCodesQuery : IRequest<IEnumerable<PromoCode>>
{
    public PromoCodeType? Type { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsExpired { get; init; }

    public Guid? CreatedById { get; init; }

    public int Skip { get; init; } = 0;

    public int Take { get; init; } = 50;

    public string? SearchTerm { get; init; }
}

/// <summary> Query to validate promo code for a product </summary>
public record ValidatePromoCodeQuery : IRequest<PromoCodeValidationResult>
{
    public string Code { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public Guid ProductId { get; init; }

    public decimal ProductPrice { get; init; }
}

/// <summary> Query to get product bundle items </summary>
public record GetProductBundleItemsQuery : IRequest<IEnumerable<ProductEntity>>
{
    public Guid ProductId { get; init; }

    public bool IncludePricing { get; init; } = true;
}

/// <summary> Query to check product access for user </summary>
public record CheckProductAccessQuery : IRequest<ProductAccessResult>
{
    public Guid UserId { get; init; }

    public Guid ProductId { get; init; }
}

// Result types
public record PromoCodeValidationResult(bool IsValid, string? Error = null, decimal? DiscountAmount = null);
public record ProductAccessResult(bool HasAccess, ProductAccessStatus? Status = null, DateTime? ExpiryDate = null);