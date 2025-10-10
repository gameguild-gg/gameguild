using System;
using System.Threading;
using System.Threading.Tasks;
using GameGuild.Core.Domain.Identity;
using GameGuild.CQRS;
using GameGuild.Database;
// using GameGuild.Modules.Contents.Domain.Entities;
using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Modules.Users;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace GameGuild.Modules.Products.Application.Features.ManageProduct;

/// <summary>
/// Command handlers for product operations
/// </summary>
public class ProductCommandHandlers
  : IRequestHandler<CreateProductCommand, CreateProductResult>,
    IRequestHandler<UpdateProductCommand, UpdateProductResult>,
    IRequestHandler<DeleteProductCommand, DeleteProductResult>,
    IRequestHandler<RestoreProductCommand, RestoreProductResult>,
    IRequestHandler<CreatePromoCodeCommand, CreatePromoCodeResult>,
    IRequestHandler<UpdatePromoCodeCommand, UpdatePromoCodeResult>,
    IRequestHandler<SetProductPricingCommand, SetProductPricingResult>,
    IRequestHandler<ApplyPromoCodeCommand, ApplyPromoCodeResult>
{
    private readonly ApplicationDbContext _context;

    private readonly IUserContext _userContext;

    private readonly ITenantContext _tenantContext;

    private readonly ILogger<ProductCommandHandlers> _logger;

    public ProductCommandHandlers(ApplicationDbContext context, IUserContext userContext, ITenantContext tenantContext, ILogger<ProductCommandHandlers> logger)
    {
        _context = context;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating product: {Name} by user {UserId}", request.Name, _userContext.UserId);

            // Validate user permissions
            if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return new CreateProductResult(false, null, "User must be authenticated"); }

            // Check if user has permission to create products (basic role check)
            if (!_userContext.IsInRole("Admin")) { return new CreateProductResult(false, null, "Unauthorized - Admin role required"); }

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Name)) { return new CreateProductResult(false, null, "Name is required"); }

            // Create the product
            var product = new Product
            {
                Id = Guid.NewGuid(),
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Set bundle items if provided
            if (request.BundleItems != null && request.BundleItems.Any()) { product.SetBundleItemIds(request.BundleItems); }

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

            return new CreateProductResult(true, product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {Name}", request.Name);

            return new CreateProductResult(false, null, $"Failed to create product: {ex.Message}");
        }
    }

    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating product: {ProductId} by user {UserId}", request.ProductId, _userContext.UserId);

            // Find the product
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null) { return new UpdateProductResult(false, "Product not found"); }

            // Check permissions - only the creator can update their product
            if (product.CreatorId != _userContext.UserId && !_userContext.IsInRole("Admin"))
            {
                return new UpdateProductResult(false, "Unauthorized - Only creator or admin can update product");
            }

            // Update fields that were provided
            if (!string.IsNullOrWhiteSpace(request.Name)) product.Name = request.Name;
            if (request.Description != null) product.Description = request.Description;
            if (request.ShortDescription != null) product.ShortDescription = request.ShortDescription;
            if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
            if (request.Type.HasValue) product.Type = request.Type.Value;
            if (request.IsBundle.HasValue) product.IsBundle = request.IsBundle.Value;
            if (request.ReferralCommissionPercentage.HasValue) product.ReferralCommissionPercentage = request.ReferralCommissionPercentage.Value;
            if (request.MaxAffiliateDiscount.HasValue) product.MaxAffiliateDiscount = request.MaxAffiliateDiscount.Value;
            if (request.AffiliateCommissionPercentage.HasValue) product.AffiliateCommissionPercentage = request.AffiliateCommissionPercentage.Value;

            // Set bundle items if provided
            if (request.BundleItems != null) { product.SetBundleItemIds(request.BundleItems); }

            product.UpdatedAt = DateTime.UtcNow;
            product.Touch();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product updated successfully: {ProductId}", product.Id);

            return new UpdateProductResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", request.ProductId);

            return new UpdateProductResult(false, $"Failed to update product: {ex.Message}");
        }
    }

    public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting product: {ProductId} (soft: {SoftDelete}) by user {UserId}",
              request.ProductId, request.SoftDelete, _userContext.UserId);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null) { return new DeleteProductResult(false, "Product not found"); }

            // Check permissions
            if (product.CreatorId != _userContext.UserId && !_userContext.IsInRole("Admin"))
            {
                return new DeleteProductResult(false, "Unauthorized - Only creator or admin can delete product");
            }

            if (request.SoftDelete)
            {
                product.Delete(request.Reason);
            }
            else
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product deleted successfully: {ProductId}", product.Id);

            return new DeleteProductResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", request.ProductId);

            return new DeleteProductResult(false, $"Failed to delete product: {ex.Message}");
        }
    }

    public async Task<RestoreProductResult> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _context.Products.IgnoreQueryFilters()
              .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null) { return new RestoreProductResult(false, "Product not found"); }

            if (!product.IsDeleted) { return new RestoreProductResult(false, "Product is not deleted"); }

            // Check permissions
            if (product.CreatorId != _userContext.UserId && !_userContext.IsInRole("Admin"))
            {
                return new RestoreProductResult(false, "Unauthorized - Only creator or admin can restore product");
            }

            product.Restore(request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            return new RestoreProductResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring product: {ProductId}", request.ProductId);
            return new RestoreProductResult(false, $"Failed to restore product: {ex.Message}");
        }
    }

    public async Task<CreatePromoCodeResult> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if promo code already exists
            var exists = await _context.PromoCodes.AnyAsync(pc => pc.Code == request.Code, cancellationToken);
            if (exists) { return new CreatePromoCodeResult(false, null, "Promo code already exists"); }

            var promoCode = new PromoCode
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Description = request.Description,
                Type = request.Type,
                DiscountPercentage = request.DiscountPercentage,
                DiscountAmount = request.DiscountAmount,
                Currency = request.Currency,
                StartDate = request.StartDate,
                ExpiryDate = request.ExpiryDate,
                MaxUses = request.MaxUses,
                MaxUsesPerUser = request.MaxUsesPerUser ?? 1,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreatePromoCodeResult(true, promoCode.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promo code: {Code}", request.Code);
            return new CreatePromoCodeResult(false, null, $"Failed to create promo code: {ex.Message}");
        }
    }

    public async Task<UpdatePromoCodeResult> Handle(UpdatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promoCode = await _context.PromoCodes.FirstOrDefaultAsync(pc => pc.Id == request.PromoCodeId, cancellationToken);
            if (promoCode == null) { return new UpdatePromoCodeResult(false, "Promo code not found"); }

            // Update fields that were provided
            if (!string.IsNullOrWhiteSpace(request.Code)) promoCode.Code = request.Code;
            if (request.Description != null) promoCode.Description = request.Description;
            if (request.Type.HasValue) promoCode.Type = request.Type.Value;
            if (request.DiscountPercentage.HasValue) promoCode.DiscountPercentage = request.DiscountPercentage.Value;
            if (request.DiscountAmount.HasValue) promoCode.DiscountAmount = request.DiscountAmount.Value;
            if (request.Currency != null) promoCode.Currency = request.Currency;
            if (request.StartDate.HasValue) promoCode.StartDate = request.StartDate.Value;
            if (request.ExpiryDate.HasValue) promoCode.ExpiryDate = request.ExpiryDate.Value;
            if (request.MaxUses.HasValue) promoCode.MaxUses = request.MaxUses.Value;
            if (request.MaxUsesPerUser.HasValue) promoCode.MaxUsesPerUser = request.MaxUsesPerUser.Value;
            if (request.IsActive.HasValue) promoCode.IsActive = request.IsActive.Value;

            promoCode.UpdatedAt = DateTime.UtcNow;
            promoCode.Touch();

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdatePromoCodeResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promo code: {PromoCodeId}", request.PromoCodeId);
            return new UpdatePromoCodeResult(false, $"Failed to update promo code: {ex.Message}");
        }
    }

    public async Task<SetProductPricingResult> Handle(SetProductPricingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _context.Products.Include(p => p.Pricing)
              .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null) { return new SetProductPricingResult(false, "Product not found"); }

            var pricing = product.Pricing.FirstOrDefault(p => p.Currency == request.Currency && p.IsDefault)
              ?? new ProductPricing { ProductId = request.ProductId, Currency = request.Currency };

            pricing.BasePrice = request.BasePrice;
            pricing.SalePrice = request.SalePrice;
            pricing.SaleStartDate = request.SaleStartDate;
            pricing.SaleEndDate = request.SaleEndDate;
            pricing.IsDefault = request.IsDefault;
            pricing.UpdatedAt = DateTime.UtcNow;

            if (pricing.Id == Guid.Empty)
            {
                pricing.Id = Guid.NewGuid();
                pricing.CreatedAt = DateTime.UtcNow;
                _context.ProductPricing.Add(pricing);
            }
            else
            {
                pricing.Touch();
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SetProductPricingResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting product pricing: {ProductId}", request.ProductId);
            return new SetProductPricingResult(false, $"Failed to set product pricing: {ex.Message}");
        }
    }

    public async Task<ApplyPromoCodeResult> Handle(ApplyPromoCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var promoCode = await _context.PromoCodes
              .Include(pc => pc.PromoCodeUses)
              .FirstOrDefaultAsync(pc => pc.Code == request.Code, cancellationToken);

            if (promoCode == null) { return new ApplyPromoCodeResult(false, null, null, "Promo code not found"); }

            if (!promoCode.IsCurrentlyValid())
            {
                return new ApplyPromoCodeResult(false, null, null, "Promo code is not currently valid");
            }

            var discountAmount = promoCode.CalculateDiscount(request.OriginalPrice);
            var finalPrice = request.OriginalPrice - discountAmount;

            // Record the usage
            var promoCodeUse = new PromoCodeUse
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promoCode.Id,
                UserId = request.UserId,
                ProductId = request.ProductId,
                UsedAt = DateTime.UtcNow,
                OriginalPrice = request.OriginalPrice,
                DiscountApplied = discountAmount,
                FinalPrice = finalPrice,
                Currency = request.Currency
            };

            _context.PromoCodeUses.Add(promoCodeUse);
            await _context.SaveChangesAsync(cancellationToken);

            return new ApplyPromoCodeResult(true, discountAmount, finalPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promo code: {Code} for user {UserId}", request.Code, request.UserId);
            return new ApplyPromoCodeResult(false, null, null, $"Failed to apply promo code: {ex.Message}");
        }
    }
}