using MediatR;
using GameGuild.Modules.Products.Models;
using GameGuild.Modules.Contents;
using GameGuild.Common;

namespace GameGuild.Modules.Products.Queries;

/// <summary>
/// Query to get product by ID
/// </summary>
public record GetProductByIdQuery : IRequest<Product?>
{
    public Guid ProductId { get; init; }
    public bool IncludePricing { get; init; } = true;
    public bool IncludePrograms { get; init; } = true;
}

/// <summary>
/// Query to get products list
/// </summary>
public record GetProductsQuery : IRequest<IEnumerable<Product>>
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

/// <summary>
/// Query to get user's products (purchased/owned)
/// </summary>
public record GetUserProductsQuery : IRequest<IEnumerable<UserProduct>>
{
    public Guid UserId { get; init; }
    public ProductAcquisitionType? AcquisitionType { get; init; }
    public bool? IsActive { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get product pricing
/// </summary>
public record GetProductPricingQuery : IRequest<IEnumerable<ProductPricing>>
{
    public Guid ProductId { get; init; }
    public string? Currency { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? EffectiveDate { get; init; }
}

/// <summary>
/// Query to get bundle contents
/// </summary>
public record GetBundleContentsQuery : IRequest<IEnumerable<Product>>
{
    public Guid BundleProductId { get; init; }
}

/// <summary>
/// Query to get product statistics
/// </summary>
public record GetProductStatsQuery : IRequest<ProductStats>
{
    public Guid? ProductId { get; init; } // If null, gets stats for all products
    public Guid? CreatorId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Query to get product revenue analytics
/// </summary>
public record GetProductRevenueQuery : IRequest<ProductRevenueReport>
{
    public Guid ProductId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string GroupBy { get; init; } = "day"; // day, week, month, year
}

/// <summary>
/// Query to check if user has access to product
/// </summary>
public record CheckUserProductAccessQuery : IRequest<UserProductAccess>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
}

/// <summary>
/// Product statistics result
/// </summary>
public record ProductStats
{
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int DraftProducts { get; init; }
    public int PublishedProducts { get; init; }
    public int TotalPurchases { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AveragePrice { get; init; }
    public int TotalUsers { get; init; }
    public IDictionary<string, int> ProductsByType { get; init; } = new Dictionary<string, int>();
    public IDictionary<string, decimal> RevenueByType { get; init; } = new Dictionary<string, decimal>();
    public IDictionary<string, int> AcquisitionsByType { get; init; } = new Dictionary<string, int>();
}

/// <summary>
/// Product revenue report
/// </summary>
public record ProductRevenueReport
{
    public Guid ProductId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalSales { get; init; }
    public decimal AverageOrderValue { get; init; }
    public IEnumerable<ProductRevenueDataPoint> DataPoints { get; init; } = Enumerable.Empty<ProductRevenueDataPoint>();
}

/// <summary>
/// Revenue data point for specific time period
/// </summary>
public record ProductRevenueDataPoint
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int SalesCount { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int NewCustomers { get; init; }
    public int ReturningCustomers { get; init; }
}

/// <summary>
/// User product access information
/// </summary>
public record UserProductAccess
{
    public bool HasAccess { get; init; }
    public UserProduct? UserProduct { get; init; }
    public ProductAcquisitionType? AcquisitionType { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? PurchasedAt { get; init; }
    public decimal? PurchasePrice { get; init; }
    public string? Currency { get; init; }
}

/// <summary>
/// Query to get products by type
/// </summary>
public record GetProductsByTypeQuery : IRequest<IEnumerable<Product>>
{
    public ProductType Type { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get published products
/// </summary>
public record GetPublishedProductsQuery : IRequest<IEnumerable<Product>>
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to search products
/// </summary>
public record SearchProductsQuery : IRequest<IEnumerable<Product>>
{
    public string SearchTerm { get; init; } = string.Empty;
    public ProductType? Type { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get products by creator
/// </summary>
public record GetProductsByCreatorQuery : IRequest<IEnumerable<Product>>
{
    public Guid CreatorId { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get products in price range
/// </summary>
public record GetProductsInPriceRangeQuery : IRequest<IEnumerable<Product>>
{
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get popular products
/// </summary>
public record GetPopularProductsQuery : IRequest<IEnumerable<Product>>
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get recent products
/// </summary>
public record GetRecentProductsQuery : IRequest<IEnumerable<Product>>
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get bundle items
/// </summary>
public record GetBundleItemsQuery : IRequest<IEnumerable<Product>>
{
    public Guid BundleId { get; init; }
}

/// <summary>
/// Query to get current pricing
/// </summary>
public record GetCurrentPricingQuery : IRequest<ProductPricing?>
{
    public Guid ProductId { get; init; }
    public string Currency { get; init; } = "USD";
}

/// <summary>
/// Query to get pricing history
/// </summary>
public record GetPricingHistoryQuery : IRequest<IEnumerable<ProductPricing>>
{
    public Guid ProductId { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get subscription plans
/// </summary>
public record GetSubscriptionPlansQuery : IRequest<IEnumerable<ProductSubscriptionPlan>>
{
    public bool ActiveOnly { get; init; } = true;
}

/// <summary>
/// Query to get specific subscription plan
/// </summary>
public record GetSubscriptionPlanQuery : IRequest<ProductSubscriptionPlan?>
{
    public Guid PlanId { get; init; }
}

/// <summary>
/// Query to check user access
/// </summary>
public record HasUserAccessQuery : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
}

/// <summary>
/// Query to get user product
/// </summary>
public record GetUserProductQuery : IRequest<UserProduct?>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
}

/// <summary>
/// Query to get product count
/// </summary>
public record GetProductCountQuery : IRequest<int>
{
    public ProductType? Type { get; init; }
    public ContentStatus? Status { get; init; }
}

/// <summary>
/// Query to get user count for product
/// </summary>
public record GetUserCountForProductQuery : IRequest<int>
{
    public Guid ProductId { get; init; }
}

/// <summary>
/// Query to get total revenue for product
/// </summary>
public record GetTotalRevenueForProductQuery : IRequest<decimal>
{
    public Guid ProductId { get; init; }
    public string Currency { get; init; } = "USD";
}
