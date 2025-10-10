using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameGuild.Core.Domain.Identity;
using GameGuild.CQRS;
using GameGuild.Database;
using ProductEntity = GameGuild.Modules.Products.Domain.Entities.Product;
using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Modules.Users;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace GameGuild.Modules.Products.Application.Features.GetProduct;

/// <summary>
/// Query handlers for product operations
/// </summary>
public class ProductQueryHandlers
  : IRequestHandler<GetProductByIdQuery, ProductEntity?>,
    IRequestHandler<GetProductsQuery, IEnumerable<ProductEntity>>,
    IRequestHandler<GetUserProductsQuery, IEnumerable<UserProduct>>,
    IRequestHandler<GetProductPricingQuery, IEnumerable<ProductPricing>>,
    IRequestHandler<GetPromoCodeQuery, PromoCode?>,
    IRequestHandler<GetPromoCodesQuery, IEnumerable<PromoCode>>,
    IRequestHandler<ValidatePromoCodeQuery, PromoCodeValidationResult>,
    IRequestHandler<GetProductBundleItemsQuery, IEnumerable<ProductEntity>>,
    IRequestHandler<CheckProductAccessQuery, ProductAccessResult>
{
    private readonly ApplicationDbContext _context;

    private readonly IUserContext _userContext;

    private readonly ITenantContext _tenantContext;

    private readonly ILogger<ProductQueryHandlers> _logger;

    public ProductQueryHandlers(ApplicationDbContext context, IUserContext userContext, ITenantContext tenantContext, ILogger<ProductQueryHandlers> logger)
    {
        _context = context;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<ProductEntity?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        if (request.IncludePricing)
        {
            query = query.Include(p => p.Pricing);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
    }

    public async Task<IEnumerable<ProductEntity>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        // Apply filters
        if (request.Type.HasValue)
        {
            query = query.Where(p => p.Type == request.Type.Value);
        }

        if (request.CreatorId.HasValue)
        {
            query = query.Where(p => p.CreatorId == request.CreatorId.Value);
        }

        if (request.IsBundle.HasValue)
        {
            query = query.Where(p => p.IsBundle == request.IsBundle.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(request.SearchTerm) ||
                                    (p.Description != null && p.Description.Contains(request.SearchTerm)) ||
                                    (p.ShortDescription != null && p.ShortDescription.Contains(request.SearchTerm)));
        }

        // Apply sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDirection?.ToUpperInvariant() == "ASC"
              ? query.OrderBy(p => p.Name)
              : query.OrderByDescending(p => p.Name),
            "createdat" or _ => request.SortDirection?.ToUpperInvariant() == "ASC"
              ? query.OrderBy(p => p.CreatedAt)
              : query.OrderByDescending(p => p.CreatedAt)
        };

        // Apply pagination
        return await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserProduct>> Handle(GetUserProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UserProducts
          .Include(up => up.Product)
          .Where(up => up.UserId == request.UserId);

        if (request.AcquisitionType.HasValue)
        {
            query = query.Where(up => up.AcquisitionType == request.AcquisitionType.Value);
        }

        if (request.IsActive.HasValue && request.IsActive.Value)
        {
            query = query.Where(up => up.AccessStatus == ProductAccessStatus.Active);
        }

        return await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductPricing>> Handle(GetProductPricingQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProductPricing.Where(pp => pp.ProductId == request.ProductId);

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            query = query.Where(pp => pp.Currency == request.Currency);
        }

        if (request.IsDefault.HasValue)
        {
            query = query.Where(pp => pp.IsDefault == request.IsDefault.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PromoCode?> Handle(GetPromoCodeQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PromoCodes.AsQueryable();

        if (request.IncludeUsages)
        {
            query = query.Include(pc => pc.PromoCodeUses);
        }

        return await query.FirstOrDefaultAsync(pc => pc.Code == request.Code, cancellationToken);
    }

    public async Task<IEnumerable<PromoCode>> Handle(GetPromoCodesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PromoCodes.AsQueryable();

        if (request.Type.HasValue)
        {
            query = query.Where(pc => pc.Type == request.Type.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(pc => pc.IsActive == request.IsActive.Value);
        }

        if (request.IsExpired.HasValue)
        {
            var now = DateTime.UtcNow;
            if (request.IsExpired.Value)
            {
                query = query.Where(pc => pc.ExpiryDate.HasValue && pc.ExpiryDate.Value < now);
            }
            else
            {
                query = query.Where(pc => !pc.ExpiryDate.HasValue || pc.ExpiryDate.Value >= now);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(pc => pc.Code.Contains(request.SearchTerm) ||
                                     (pc.Description != null && pc.Description.Contains(request.SearchTerm)));
        }

        return await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);
    }

    public async Task<PromoCodeValidationResult> Handle(ValidatePromoCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var promoCode = await _context.PromoCodes
              .Include(pc => pc.PromoCodeUses)
              .FirstOrDefaultAsync(pc => pc.Code == request.Code, cancellationToken);

            if (promoCode == null)
            {
                return new PromoCodeValidationResult(false, "Promo code not found");
            }

            if (!promoCode.IsCurrentlyValid())
            {
                return new PromoCodeValidationResult(false, "Promo code is not currently valid");
            }

            // Check user usage limits
            var userUsages = promoCode.PromoCodeUses.Count(u => u.UserId == request.UserId);
            if (userUsages >= promoCode.MaxUsesPerUser)
            {
                return new PromoCodeValidationResult(false, "Promo code usage limit exceeded for this user");
            }

            var discountAmount = promoCode.CalculateDiscount(request.ProductPrice);
            return new PromoCodeValidationResult(true, null, discountAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code: {Code}", request.Code);
            return new PromoCodeValidationResult(false, "Error validating promo code");
        }
    }

    public async Task<IEnumerable<ProductEntity>> Handle(GetProductBundleItemsQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null || !product.IsBundle)
        {
            return Enumerable.Empty<ProductEntity>();
        }

        var bundleItemIds = product.GetBundleItemIds();
        if (!bundleItemIds.Any())
        {
            return Enumerable.Empty<ProductEntity>();
        }

        var query = _context.Products.Where(p => bundleItemIds.Contains(p.Id));

        if (request.IncludePricing)
        {
            query = query.Include(p => p.Pricing);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ProductAccessResult> Handle(CheckProductAccessQuery request, CancellationToken cancellationToken)
    {
        var userProduct = await _context.UserProducts
          .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.ProductId == request.ProductId, cancellationToken);

        if (userProduct == null)
        {
            return new ProductAccessResult(false);
        }

        var hasAccess = userProduct.AccessStatus == ProductAccessStatus.Active;
        return new ProductAccessResult(hasAccess, userProduct.AccessStatus, userProduct.ExpiryDate);
    }
}