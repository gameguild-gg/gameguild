using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Enums;
using GameGuild.Common.Entities;
using GameGuild.Modules.Product.Models;
using ProductEntity = GameGuild.Modules.Product.Models.Product;


namespace GameGuild.Modules.Product.Services;

/// <summary>
/// Service implementation for Product business logic
/// Provides operations for managing products, pricing, subscriptions, and access control
/// </summary>
public class ProductService(ApplicationDbContext context) : IProductService {
  // Basic CRUD operations
  public async Task<ProductEntity?> GetProductByIdAsync(Guid id) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null)
                         .FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task<ProductEntity?> GetProductByIdWithDetailsAsync(Guid id) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Include(p => p.SubscriptionPlans)
                         .Include(p => p.UserProducts)
                         .Include(p => p.PromoCodes)
                         .Include(p => p.ProductPrograms)
                         .Where(p => p.DeletedAt == null)
                         .FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task<IEnumerable<ProductEntity>> GetProductsAsync(int skip = 0, int take = 50) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null)
                         .OrderBy(p => p.Name)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<IEnumerable<ProductEntity>> GetProductsByTypeAsync(ProductType type, int skip = 0, int take = 50) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && p.Type == type)
                         .OrderBy(p => p.Name)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<IEnumerable<ProductEntity>> GetPublishedProductsAsync(int skip = 0, int take = 50) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null &&
                                     p.Status == ContentStatus.Published &&
                                     p.Visibility == Common.Entities.AccessLevel.Public
                         )
                         .OrderBy(p => p.Name)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<ProductEntity> CreateProductAsync(ProductEntity product) {
    product.Status = ContentStatus.Draft;
    product.Visibility = Common.Entities.AccessLevel.Private;

    context.Products.Add(product);
    await context.SaveChangesAsync();

    return product;
  }

  public async Task<ProductEntity> UpdateProductAsync(ProductEntity product) {
    product.Touch();
    context.Products.Update(product);
    await context.SaveChangesAsync();

    return product;
  }

  public async Task DeleteProductAsync(Guid id) {
    var product = await context.Products.FindAsync(id);

    if (product != null) {
      product.SoftDelete();
      await context.SaveChangesAsync();
    }
  }

  public async Task<bool> ProductExistsAsync(Guid id) { return await context.Products.Where(p => p.DeletedAt == null).AnyAsync(p => p.Id == id); }

  // Visibility and publishing
  public async Task<ProductEntity> PublishProductAsync(Guid id) {
    var product = await GetProductByIdAsync(id);

    if (product == null) throw new ArgumentException("Product not found", nameof(id));

    product.Status = ContentStatus.Published;
    product.Touch();
    await context.SaveChangesAsync();

    return product;
  }

  public async Task<ProductEntity> UnpublishProductAsync(Guid id) {
    var product = await GetProductByIdAsync(id);

    if (product == null) throw new ArgumentException("Product not found", nameof(id));

    product.Status = ContentStatus.Draft;
    product.Touch();
    await context.SaveChangesAsync();

    return product;
  }

  public async Task<ProductEntity> ArchiveProductAsync(Guid id) {
    var product = await GetProductByIdAsync(id);

    if (product == null) throw new ArgumentException("Product not found", nameof(id));

    product.Status = ContentStatus.Archived;
    product.Touch();
    await context.SaveChangesAsync();

    return product;
  }

  public async Task<ProductEntity> SetVisibilityAsync(Guid id, Common.Entities.AccessLevel visibility) {
    var product = await GetProductByIdAsync(id);

    if (product == null) throw new ArgumentException("Product not found", nameof(id));

    product.Visibility = visibility;
    product.Touch();
    await context.SaveChangesAsync();

    return product;
  }

  // Bundle management
  public async Task<IEnumerable<ProductEntity>> GetBundleItemsAsync(Guid bundleId) {
    var bundle = await GetProductByIdAsync(bundleId);

    if (bundle == null || !bundle.IsBundle) return [];

    var bundleItemIds = bundle.GetBundleItemIds();

    if (!bundleItemIds.Any()) return [];

    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && bundleItemIds.Contains(p.Id))
                         .OrderBy(p => p.Name)
                         .ToListAsync();
  }

  public async Task<ProductEntity> AddToBundleAsync(Guid bundleId, Guid productId) {
    var bundle = await GetProductByIdAsync(bundleId);

    if (bundle == null) throw new ArgumentException("Bundle not found", nameof(bundleId));

    if (!bundle.IsBundle) throw new InvalidOperationException("Product is not a bundle");

    var product = await ProductExistsAsync(productId);

    if (!product) throw new ArgumentException("Product not found", nameof(productId));

    var bundleItemIds = bundle.GetBundleItemIds();

    if (!bundleItemIds.Contains(productId)) {
      bundleItemIds.Add(productId);
      bundle.SetBundleItemIds(bundleItemIds);
      bundle.Touch();
      await context.SaveChangesAsync();
    }

    return bundle;
  }

  public async Task<ProductEntity> RemoveFromBundleAsync(Guid bundleId, Guid productId) {
    var bundle = await GetProductByIdAsync(bundleId);

    if (bundle == null) throw new ArgumentException("Bundle not found", nameof(bundleId));

    if (!bundle.IsBundle) throw new InvalidOperationException("Product is not a bundle");

    var bundleItemIds = bundle.GetBundleItemIds();

    if (bundleItemIds.Remove(productId)) {
      bundle.SetBundleItemIds(bundleItemIds);
      bundle.Touch();
      await context.SaveChangesAsync();
    }

    return bundle;
  }

  public async Task<bool> IsProductInBundleAsync(Guid bundleId, Guid productId) {
    var bundle = await GetProductByIdAsync(bundleId);

    if (bundle == null || !bundle.IsBundle) return false;

    var bundleItemIds = bundle.GetBundleItemIds();

    return bundleItemIds.Contains(productId);
  }

  // Pricing management
  public async Task<ProductPricing?> GetCurrentPricingAsync(Guid productId) {
    return await context.ProductPricings.Where(pp => pp.DeletedAt == null && pp.ProductId == productId && pp.IsDefault)
                         .OrderByDescending(pp => pp.CreatedAt)
                         .FirstOrDefaultAsync();
  }

  public async Task<IEnumerable<ProductPricing>> GetPricingHistoryAsync(Guid productId) {
    return await context.ProductPricings.Where(pp => pp.DeletedAt == null && pp.ProductId == productId)
                         .OrderByDescending(pp => pp.CreatedAt)
                         .ToListAsync();
  }

  public async Task<ProductPricing> SetPricingAsync(Guid productId, decimal basePrice, string currency = "USD") {
    var product = await ProductExistsAsync(productId);

    if (!product) throw new ArgumentException("Product not found", nameof(productId));

    // Set existing default pricing to non-default
    var existingDefault = await GetCurrentPricingAsync(productId);

    if (existingDefault != null) {
      existingDefault.IsDefault = false;
      existingDefault.Touch();
    }

    // Create new pricing
    var pricing = new ProductPricing {
      ProductId = productId,
      Name = "Standard",
      BasePrice = basePrice,
      Currency = currency,
      IsDefault = true,
    };

    context.ProductPricings.Add(pricing);
    await context.SaveChangesAsync();

    return pricing;
  }

  public async Task<ProductPricing> UpdatePricingAsync(Guid pricingId, decimal basePrice) {
    var pricing = await context.ProductPricings.Where(pp => pp.DeletedAt == null)
                                .FirstOrDefaultAsync(pp => pp.Id == pricingId);

    if (pricing == null) throw new ArgumentException("Pricing not found", nameof(pricingId));

    pricing.BasePrice = basePrice;
    pricing.Touch();
    await context.SaveChangesAsync();

    return pricing;
  }

  // Subscription management
  public async Task<IEnumerable<ProductSubscriptionPlan>> GetSubscriptionPlansAsync(Guid productId) {
    return await context.ProductSubscriptionPlans.Where(psp => !psp.IsDeleted && psp.ProductId == productId)
                         .OrderBy(psp => psp.Name)
                         .ToListAsync();
  }

  public async Task<ProductSubscriptionPlan?> GetSubscriptionPlanAsync(Guid planId) {
    return await context.ProductSubscriptionPlans.Where(psp => !psp.IsDeleted)
                         .FirstOrDefaultAsync(psp => psp.Id == planId);
  }

  public async Task<ProductSubscriptionPlan> CreateSubscriptionPlanAsync(ProductSubscriptionPlan plan) {
    context.ProductSubscriptionPlans.Add(plan);
    await context.SaveChangesAsync();

    return plan;
  }

  public async Task<ProductSubscriptionPlan> UpdateSubscriptionPlanAsync(ProductSubscriptionPlan plan) {
    plan.Touch();
    context.ProductSubscriptionPlans.Update(plan);
    await context.SaveChangesAsync();

    return plan;
  }

  public async Task DeleteSubscriptionPlanAsync(Guid planId) {
    var plan = await context.ProductSubscriptionPlans.FindAsync(planId);

    if (plan != null) {
      plan.SoftDelete();
      await context.SaveChangesAsync();
    }
  }

  // User access and ownership
  public async Task<bool> HasUserAccessAsync(Guid userId, Guid productId) {
    return await context.UserProducts.Where(up =>
                                               !up.IsDeleted &&
                                               up.UserId == userId &&
                                               up.ProductId == productId &&
                                               up.AccessStatus == ProductAccessStatus.Active &&
                                               (up.AccessEndDate == null || up.AccessEndDate > DateTime.UtcNow)
                         )
                         .AnyAsync();
  }

  public async Task<UserProduct?> GetUserProductAsync(Guid userId, Guid productId) {
    return await context.UserProducts.Include(up => up.User)
                         .Include(up => up.Product)
                         .Where(up => !up.IsDeleted && up.UserId == userId && up.ProductId == productId)
                         .FirstOrDefaultAsync();
  }

  public async Task<IEnumerable<UserProduct>> GetUserProductsAsync(Guid userId) {
    return await context.UserProducts.Include(up => up.Product)
                         .Where(up => !up.IsDeleted && up.UserId == userId)
                         .OrderByDescending(up => up.CreatedAt)
                         .ToListAsync();
  }

  public async Task<UserProduct> GrantUserAccessAsync(
    Guid userId, Guid productId,
    ProductAcquisitionType acquisitionType, decimal purchasePrice = 0, string currency = "USD",
    DateTime? expiresAt = null
  ) {
    // Check if user already has access
    var existingAccess = await GetUserProductAsync(userId, productId);

    if (existingAccess != null && existingAccess.AccessStatus == ProductAccessStatus.Active) return existingAccess;

    var userProduct = new UserProduct {
      UserId = userId,
      ProductId = productId,
      AcquisitionType = acquisitionType,
      AccessStatus = ProductAccessStatus.Active,
      PricePaid = purchasePrice,
      Currency = currency,
      AccessEndDate = expiresAt,
    };

    context.UserProducts.Add(userProduct);
    await context.SaveChangesAsync();

    return userProduct;
  }

  public async Task RevokeUserAccessAsync(Guid userId, Guid productId) {
    var userProduct = await GetUserProductAsync(userId, productId);

    if (userProduct != null) {
      userProduct.AccessStatus = ProductAccessStatus.Revoked;
      userProduct.Touch();
      await context.SaveChangesAsync();
    }
  }

  // Promo code management
  public async Task<PromoCode?> GetPromoCodeAsync(string code) {
    return await context.PromoCodes.Include(pc => pc.Product)
                         .Where(pc => !pc.IsDeleted && pc.Code == code)
                         .FirstOrDefaultAsync();
  }

  public async Task<PromoCode> CreatePromoCodeAsync(PromoCode promoCode) {
    context.PromoCodes.Add(promoCode);
    await context.SaveChangesAsync();

    return promoCode;
  }

  public async Task<PromoCode> UpdatePromoCodeAsync(PromoCode promoCode) {
    promoCode.Touch();
    context.PromoCodes.Update(promoCode);
    await context.SaveChangesAsync();

    return promoCode;
  }

  public async Task DeletePromoCodeAsync(Guid id) {
    var promoCode = await context.PromoCodes.FindAsync(id);

    if (promoCode != null) {
      promoCode.SoftDelete();
      await context.SaveChangesAsync();
    }
  }

  public async Task<PromoCodeUse> UsePromoCodeAsync(Guid userId, string code, decimal discountAmount) {
    var promoCode = await GetPromoCodeAsync(code);

    if (promoCode == null) throw new ArgumentException("Promo code not found", nameof(code));

    if (!await IsPromoCodeValidAsync(code)) throw new InvalidOperationException("Promo code is not valid");

    var promoCodeUse = new PromoCodeUse {
      PromoCodeId = promoCode.Id,
      UserId = userId,
      FinancialTransactionId = Guid.NewGuid(), // This should be set to actual transaction ID
      DiscountApplied = discountAmount,
    };

    context.PromoCodeUses.Add(promoCodeUse);

    // The usage count is managed by the collection, not a separate field
    promoCode.Touch();

    await context.SaveChangesAsync();

    return promoCodeUse;
  }

  public async Task<bool> IsPromoCodeValidAsync(string code, Guid? productId = null) {
    var promoCode = await GetPromoCodeAsync(code);

    if (promoCode == null) return false;

    var now = DateTime.UtcNow;

    // Check if promo code is within valid date range
    if (promoCode.ValidFrom.HasValue && now < promoCode.ValidFrom.Value) return false;

    if (promoCode.ValidUntil.HasValue && now > promoCode.ValidUntil.Value) return false;

    // Check usage limits
    if (promoCode.MaxUses.HasValue) {
      var currentUsageCount = await context.PromoCodeUses
                                            .Where(pcu => !pcu.IsDeleted && pcu.PromoCodeId == promoCode.Id)
                                            .CountAsync();

      if (currentUsageCount >= promoCode.MaxUses.Value) return false;
    }

    // Check product-specific restrictions
    if (productId.HasValue && promoCode.ProductId.HasValue && promoCode.ProductId.Value != productId.Value) return false;

    return true;
  }

  // Analytics and statistics
  public async Task<int> GetProductCountAsync(ProductType? type = null, Common.Entities.AccessLevel? visibility = null) {
    var query = context.Products.Where(p => p.DeletedAt == null);

    if (type.HasValue) query = query.Where(p => p.Type == type.Value);

    if (visibility.HasValue) query = query.Where(p => p.Visibility == visibility.Value);

    return await query.CountAsync();
  }

  public async Task<int> GetUserCountForProductAsync(Guid productId) {
    return await context.UserProducts
                         .Where(up => !up.IsDeleted && up.ProductId == productId && up.AccessStatus == ProductAccessStatus.Active)
                         .CountAsync();
  }

  public async Task<decimal> GetTotalRevenueForProductAsync(Guid productId) {
    return await context.UserProducts
                         .Where(up => !up.IsDeleted && up.ProductId == productId && up.AccessStatus == ProductAccessStatus.Active)
                         .SumAsync(up => up.PricePaid);
  }

  public async Task<IEnumerable<ProductEntity>> GetPopularProductsAsync(int count = 10) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                         .OrderByDescending(p =>
                                              p.UserProducts.Count(up => !up.IsDeleted && up.AccessStatus == ProductAccessStatus.Active)
                         )
                         .Take(count)
                         .ToListAsync();
  }

  public async Task<IEnumerable<ProductEntity>> GetRecentProductsAsync(int count = 10) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                         .OrderByDescending(p => p.CreatedAt)
                         .Take(count)
                         .ToListAsync();
  }

  // Search and filtering
  public async Task<IEnumerable<ProductEntity>> SearchProductsAsync(string searchTerm, int skip = 0, int take = 50) {
    if (string.IsNullOrWhiteSpace(searchTerm)) return await GetProductsAsync(skip, take);

    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null &&
                                     (p.Name.Contains(searchTerm) ||
                                      (p.ShortDescription != null && p.ShortDescription.Contains(searchTerm)))
                         )
                         .OrderBy(p => p.Name)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<IEnumerable<ProductEntity>> GetProductsByCreatorAsync(Guid creatorId, int skip = 0, int take = 50) {
    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && p.CreatorId == creatorId)
                         .OrderByDescending(p => p.CreatedAt)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }

  public async Task<IEnumerable<ProductEntity>> GetProductsInPriceRangeAsync(
    decimal minPrice, decimal maxPrice,
    string currency = "USD", int skip = 0, int take = 50
  ) {
    var productIds = await context.ProductPricings
                                   .Where(pp => pp.DeletedAt == null &&
                                                pp.IsDefault &&
                                                pp.Currency == currency &&
                                                pp.BasePrice >= minPrice &&
                                                pp.BasePrice <= maxPrice
                                   )
                                   .Select(pp => pp.ProductId)
                                   .ToListAsync();

    return await context.Products.Include(p => p.Creator)
                         .Include(p => p.ProductPricings)
                         .Where(p => p.DeletedAt == null && productIds.Contains(p.Id))
                         .OrderBy(p => p.Name)
                         .Skip(skip)
                         .Take(take)
                         .ToListAsync();
  }
}
