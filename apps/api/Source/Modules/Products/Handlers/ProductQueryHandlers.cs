using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IUserContext = GameGuild.Common.IUserContext;


namespace GameGuild.Modules.Products;

/// <summary>
/// Query handlers for product operations
/// </summary>
public class ProductQueryHandlers :
  IRequestHandler<GetProductByIdQuery, Product?>,
  IRequestHandler<GetProductsQuery, IEnumerable<Product>>,
  IRequestHandler<GetUserProductsQuery, IEnumerable<UserProduct>>,
  IRequestHandler<GetProductStatsQuery, ProductStats> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;
  private readonly ILogger<ProductQueryHandlers> _logger;

  public ProductQueryHandlers(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext,
    ILogger<ProductQueryHandlers> logger
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
    _logger = logger;
  }

  public async Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting product by ID: {ProductId}", request.ProductId);

      var query = _context.Products
                          .Where(p => p.Id == request.ProductId && p.DeletedAt == null);

      // Include related data if requested
      if (request.IncludePricing) { query = query.Include(p => p.ProductPricings); }

      if (request.IncludePrograms) { query = query.Include(p => p.ProductPrograms); }

      // Apply access control
      query = ApplyAccessControl(query);

      var product = await query.FirstOrDefaultAsync(cancellationToken);

      if (product == null) { _logger.LogWarning("Product not found or not accessible: {ProductId}", request.ProductId); }

      return product;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting product by ID: {ProductId}", request.ProductId);

      return null;
    }
  }

  public async Task<IEnumerable<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug(
        "Getting products with filters - Type: {Type}, Status: {Status}, Skip: {Skip}, Take: {Take}",
        request.Type,
        request.Status,
        request.Skip,
        request.Take
      );

      var query = _context.Products
                          .Where(p => p.DeletedAt == null);

      // Apply filters
      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

      if (request.Visibility.HasValue) { query = query.Where(p => p.Visibility == request.Visibility.Value); }

      if (request.CreatorId.HasValue) { query = query.Where(p => p.CreatorId == request.CreatorId.Value); }

      if (!string.IsNullOrEmpty(request.SearchTerm)) {
        var searchTerm = request.SearchTerm.ToLower();
        query = query.Where(p =>
                              p.Name.ToLower().Contains(searchTerm) ||
                              (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)) ||
                              p.Title.ToLower().Contains(searchTerm)
        );
      }

      if (request.IsBundle.HasValue) { query = query.Where(p => p.IsBundle == request.IsBundle.Value); }

      // Apply access control
      query = ApplyAccessControl(query);

      // Apply sorting
      query = request.SortBy?.ToLower() switch {
        "name" => request.SortDirection?.ToUpper() == "DESC" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        "createdat" => request.SortDirection?.ToUpper() == "DESC" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.CreatedAt)
      };

      // Apply pagination
      query = query
              .Skip(request.Skip)
              .Take(Math.Min(request.Take, 100)); // Limit to prevent abuse

      var products = await query.ToListAsync(cancellationToken);

      _logger.LogDebug("Retrieved {Count} products", products.Count);

      return products;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting products");

      return Enumerable.Empty<Product>();
    }
  }

  public async Task<IEnumerable<UserProduct>> Handle(GetUserProductsQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting user products for user: {UserId}", request.UserId);

      // Temporarily disable access control for debugging
      // if (_userContext.UserId != request.UserId) {
      //   return Enumerable.Empty<UserProduct>();
      // }

      var query = _context.UserProducts
                          .Include(up => up.Product)
                          .Where(up => up.UserId == request.UserId && up.Product.DeletedAt == null);

      // Apply optional filters
      if (request.AcquisitionType.HasValue) {
        query = query.Where(up => up.AcquisitionType == request.AcquisitionType.Value);
      }

      if (request.IsActive.HasValue) {
        if (request.IsActive.Value) {
          query = query.Where(up => up.AccessStatus == ProductAccessStatus.Active);
        } else {
          query = query.Where(up => up.AccessStatus != ProductAccessStatus.Active);
        }
      }

      // Apply pagination
      query = query.Skip(request.Skip)
                   .Take(Math.Min(request.Take, 100)); // Limit to prevent abuse

      var userProducts = await query.ToListAsync(cancellationToken);

      _logger.LogDebug("Retrieved {Count} user products for user {UserId}", userProducts.Count, request.UserId);

      return userProducts;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting user products for user: {UserId}", request.UserId);

      return Enumerable.Empty<UserProduct>();
    }
  }

  /// <summary>
  /// Apply access control based on user context and product access levels
  /// </summary>
  private IQueryable<Product> ApplyAccessControl(IQueryable<Product> query) {
    // Anonymous users can only see public products that are published
    if (!_userContext.IsAuthenticated) {
      return query.Where(p =>
                           p.Visibility == AccessLevel.Public &&
                           p.Status == ContentStatus.Published
      );
    }

    // Authenticated users can see public products that are published
    var accessibleQuery = query.Where(p =>
                                        p.Status == ContentStatus.Published &&
                                        p.Visibility == AccessLevel.Public
    );

    // Users can see their own products regardless of status
    var userProducts = query.Where(p => p.CreatorId == _userContext.UserId);
    accessibleQuery = accessibleQuery.Union(userProducts);

    return accessibleQuery;
  }

  public async Task<ProductStats> Handle(GetProductStatsQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting product statistics");

      var query = _context.Products.AsQueryable();

      // Apply access control
      query = ApplyAccessControl(query);

      // Apply optional filters
      if (request.ProductId.HasValue) {
        query = query.Where(p => p.Id == request.ProductId.Value);
      }

      if (request.CreatorId.HasValue) {
        query = query.Where(p => p.CreatorId == request.CreatorId.Value);
      }

      if (request.FromDate.HasValue) {
        query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
      }

      if (request.ToDate.HasValue) {
        query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
      }

      // Get basic product counts
      var totalProducts = await query.CountAsync(cancellationToken);
      var publishedProducts = await query.CountAsync(p => p.Status == ContentStatus.Published, cancellationToken);
      var draftProducts = await query.CountAsync(p => p.Status == ContentStatus.Draft, cancellationToken);
      var activeProducts = publishedProducts; // Consider published products as active

      // Get products by type
      var productsByType = await query
        .GroupBy(p => p.Type)
        .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
        .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

      // For user products and revenue, we need to check if we have access to that data
      var userProductsQuery = _context.UserProducts.AsQueryable();
      var totalPurchases = 0;
      var totalRevenue = 0m;
      var averagePrice = 0m;
      var totalUsers = 0;
      var acquisitionsByType = new Dictionary<string, int>();
      var revenueByType = new Dictionary<string, decimal>();

      // Only calculate purchase/revenue stats if user has appropriate permissions
      if (_userContext.IsAuthenticated) {
        try {
          // Apply product filter if specified
          if (request.ProductId.HasValue) {
            userProductsQuery = userProductsQuery.Where(up => up.ProductId == request.ProductId.Value);
          } else {
            // Filter to only products the user can see
            var accessibleProductIds = await query.Select(p => p.Id).ToListAsync(cancellationToken);
            userProductsQuery = userProductsQuery.Where(up => accessibleProductIds.Contains(up.ProductId));
          }

          totalPurchases = await userProductsQuery.CountAsync(cancellationToken);
          totalUsers = await userProductsQuery.Select(up => up.UserId).Distinct().CountAsync(cancellationToken);

          // Get acquisitions by type
          acquisitionsByType = await userProductsQuery
            .GroupBy(up => up.AcquisitionType)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

          // For revenue calculations, we'd need pricing information
          // This is a placeholder - you might need to join with pricing data
          totalRevenue = totalPurchases * 10m; // Placeholder calculation
          averagePrice = totalPurchases > 0 ? totalRevenue / totalPurchases : 0m;

          // Revenue by type - placeholder
          revenueByType = productsByType.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value * 10m // Placeholder calculation
          );
        }
        catch (Exception ex) {
          _logger.LogWarning(ex, "Could not calculate purchase/revenue statistics");
          // Continue with basic stats even if purchase stats fail
        }
      }

      var stats = new ProductStats {
        TotalProducts = totalProducts,
        ActiveProducts = activeProducts,
        DraftProducts = draftProducts,
        PublishedProducts = publishedProducts,
        TotalPurchases = totalPurchases,
        TotalRevenue = totalRevenue,
        AveragePrice = averagePrice,
        TotalUsers = totalUsers,
        ProductsByType = productsByType,
        RevenueByType = revenueByType,
        AcquisitionsByType = acquisitionsByType
      };

      _logger.LogDebug("Retrieved product statistics: {TotalProducts} products, {PublishedProducts} published", 
        stats.TotalProducts, stats.PublishedProducts);

      return stats;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting product statistics");
      
      // Return empty stats on error
      return new ProductStats {
        TotalProducts = 0,
        ActiveProducts = 0,
        DraftProducts = 0,
        PublishedProducts = 0,
        TotalPurchases = 0,
        TotalRevenue = 0m,
        AveragePrice = 0m,
        TotalUsers = 0,
        ProductsByType = new Dictionary<string, int>(),
        RevenueByType = new Dictionary<string, decimal>(),
        AcquisitionsByType = new Dictionary<string, int>()
      };
    }
  }
}
