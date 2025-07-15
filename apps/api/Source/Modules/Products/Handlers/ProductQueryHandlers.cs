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
  IRequestHandler<GetUserProductsQuery, IEnumerable<UserProduct>> {
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
                          .Where(p => p.Id == request.ProductId && !p.IsDeleted);

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
                          .Where(p => !p.IsDeleted);

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

      var query = _context.UserProducts
                          .Include(up => up.Product)
                          .Where(up => up.UserId == request.UserId && !up.Product.IsDeleted);

      // Apply access control - users can only see their own products
      if (_userContext.UserId != request.UserId) {
        return Enumerable.Empty<UserProduct>();
      }

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
}
