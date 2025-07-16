using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
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
  IRequestHandler<DeleteProductCommand, DeleteProductResult>,
  IRequestHandler<PublishProductCommand, PublishProductResult>,
  IRequestHandler<UnpublishProductCommand, UnpublishProductResult> {
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

      // Check if user has permission to create products (basic role check)
      if (!_userContext.IsInRole("Admin")) { return new CreateProductResult { Success = false, ErrorMessage = "Unauthorized - Admin role required" }; }

      // Basic validation
      if (string.IsNullOrWhiteSpace(request.Name)) { return new CreateProductResult { Success = false, ErrorMessage = "Name is required" }; }

      // Create the product
      var product = new Product {
        Id = Guid.NewGuid(),
        Title = request.Name, // Using Title from Resource base class
        Name = request.Name,
        Description = request.Description,
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

      // Check permissions - only the creator can update their product
      if (product.CreatorId != _userContext.UserId) { return new UpdateProductResult { Success = false, ErrorMessage = "User does not have permission to update this product" }; }

      // Update properties
      if (!string.IsNullOrEmpty(request.Name)) {
        product.Name = request.Name;
        product.Title = request.Name; // Update Resource.Title as well
      }

      if (request.Description != null) product.Description = request.Description;

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

      // Check permissions - only the creator can delete their product
      if (product.CreatorId != _userContext.UserId) { return new DeleteProductResult { Success = false, ErrorMessage = "User does not have permission to delete this product" }; }

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

  public async Task<PublishProductResult> Handle(PublishProductCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Publishing product: {ProductId} by user {UserId}", request.ProductId, _userContext.UserId);

      var product = await _context.Products
                                  .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

      if (product == null) { return new PublishProductResult { Success = false, ErrorMessage = "Product not found" }; }

      // Check permissions - only the creator can publish their product
      if (product.CreatorId != _userContext.UserId) { return new PublishProductResult { Success = false, ErrorMessage = "User does not have permission to publish this product" }; }

      // Update status to published
      product.Status = ContentStatus.Published;
      product.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Product published successfully: {ProductId}", product.Id);

      return new PublishProductResult { Success = true, Product = product };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error publishing product: {ProductId}", request.ProductId);

      return new PublishProductResult { Success = false, ErrorMessage = $"Failed to publish product: {ex.Message}" };
    }
  }

  public async Task<UnpublishProductResult> Handle(UnpublishProductCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Unpublishing product: {ProductId} by user {UserId}", request.ProductId, _userContext.UserId);

      var product = await _context.Products
                                  .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

      if (product == null) { return new UnpublishProductResult { Success = false, ErrorMessage = "Product not found" }; }

      // Check permissions - only the creator can unpublish their product
      if (product.CreatorId != _userContext.UserId) { return new UnpublishProductResult { Success = false, ErrorMessage = "User does not have permission to unpublish this product" }; }

      // Update status to draft
      product.Status = ContentStatus.Draft;
      product.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Product unpublished successfully: {ProductId}", product.Id);

      return new UnpublishProductResult { Success = true, Product = product };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error unpublishing product: {ProductId}", request.ProductId);

      return new UnpublishProductResult { Success = false, ErrorMessage = $"Failed to unpublish product: {ex.Message}" };
    }
  }
}
