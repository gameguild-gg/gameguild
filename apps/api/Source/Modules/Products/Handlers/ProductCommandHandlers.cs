using GameGuild.Common.Interfaces;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IUserContext = GameGuild.Common.IUserContext;


namespace GameGuild.Modules.Products;

/// <summary>
/// Command handlers for product operations
/// </summary>
public class ProductCommandHandlers :
  IRequestHandler<CreateProductCommand, CreateProductResult>,
  IRequestHandler<UpdateProductCommand, UpdateProductResult>,
  IRequestHandler<DeleteProductCommand, DeleteProductResult> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;
  private readonly ILogger<ProductCommandHandlers> _logger;

  public ProductCommandHandlers(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext,
    ILogger<ProductCommandHandlers> logger
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
    _logger = logger;
  }

  public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Creating product: {Name} by user {UserId}", request.Name, _userContext.UserId);

      // Validate user permissions
      if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return new CreateProductResult { Success = false, ErrorMessage = "User must be authenticated" }; }

      // Check if user can create products
      if (!_userContext.IsInRole("Admin") && !_userContext.IsInRole("ContentCreator")) { return new CreateProductResult { Success = false, ErrorMessage = "User does not have permission to create products" }; }

      // Create the product
      var product = new Product {
        Id = Guid.NewGuid(),
        Title = request.Name, // Using Title from Resource base class
        Name = request.Name,
        ShortDescription = request.ShortDescription,
        ImageUrl = request.ImageUrl,
        Type = request.Type,
        IsBundle = request.IsBundle,
        CreatorId = request.CreatorId,
        ReferralCommissionPercentage = request.ReferralCommissionPercentage,
        MaxAffiliateDiscount = request.MaxAffiliateDiscount,
        AffiliateCommissionPercentage = request.AffiliateCommissionPercentage,
        Visibility = request.Visibility,
        Status = request.Status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      // Set bundle items if provided
      if (request.BundleItems != null && request.BundleItems.Any()) { product.SetBundleItemIds(request.BundleItems); }

      _context.Products.Add(product);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

      return new CreateProductResult { Success = true, Product = product };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error creating product: {Name}", request.Name);

      return new CreateProductResult { Success = false, ErrorMessage = $"Failed to create product: {ex.Message}" };
    }
  }

  public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Updating product: {ProductId} by user {UserId}", request.ProductId, _userContext.UserId);

      // Find the product
      var product = await _context.Products
                                  .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

      if (product == null) { return new UpdateProductResult { Success = false, ErrorMessage = "Product not found" }; }

      // Check permissions
      if (!_userContext.IsInRole("Admin") && product.CreatorId != _userContext.UserId) { return new UpdateProductResult { Success = false, ErrorMessage = "User does not have permission to update this product" }; }

      // Update properties
      if (!string.IsNullOrEmpty(request.Name)) {
        product.Name = request.Name;
        product.Title = request.Name; // Update Resource.Title as well
      }

      if (request.ShortDescription != null) product.ShortDescription = request.ShortDescription;

      if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;

      if (request.Type.HasValue) product.Type = request.Type.Value;

      if (request.IsBundle.HasValue) product.IsBundle = request.IsBundle.Value;

      if (request.BundleItems != null) product.SetBundleItemIds(request.BundleItems);

      if (request.ReferralCommissionPercentage.HasValue) product.ReferralCommissionPercentage = request.ReferralCommissionPercentage.Value;

      if (request.MaxAffiliateDiscount.HasValue) product.MaxAffiliateDiscount = request.MaxAffiliateDiscount.Value;

      if (request.AffiliateCommissionPercentage.HasValue) product.AffiliateCommissionPercentage = request.AffiliateCommissionPercentage.Value;

      if (request.Visibility.HasValue) product.Visibility = request.Visibility.Value;

      if (request.Status.HasValue) product.Status = request.Status.Value;

      product.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Product updated successfully: {ProductId}", product.Id);

      return new UpdateProductResult { Success = true, Product = product };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating product: {ProductId}", request.ProductId);

      return new UpdateProductResult { Success = false, ErrorMessage = $"Failed to update product: {ex.Message}" };
    }
  }

  public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Deleting product: {ProductId} by user {UserId}", request.ProductId, _userContext.UserId);

      var product = await _context.Products
                                  .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

      if (product == null) { return new DeleteProductResult { Success = false, ErrorMessage = "Product not found" }; }

      // Check permissions
      if (!_userContext.IsInRole("Admin") && product.CreatorId != _userContext.UserId) { return new DeleteProductResult { Success = false, ErrorMessage = "User does not have permission to delete this product" }; }

      // Soft delete using Entity base class method
      product.SoftDelete();

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Product deleted successfully: {ProductId}", product.Id);

      return new DeleteProductResult { Success = true };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error deleting product: {ProductId}", request.ProductId);

      return new DeleteProductResult { Success = false, ErrorMessage = $"Failed to delete product: {ex.Message}" };
    }
  }
}
