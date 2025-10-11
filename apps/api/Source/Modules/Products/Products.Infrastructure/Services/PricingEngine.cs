using GameGuild.Modules.Products.Application.Services;
using GameGuild.Database;
using GameGuild.Modules.Products.Domain.Entities;


namespace GameGuild.Modules.Products.Infrastructure.Services;

/// <summary>Implementation of dynamic pricing engine</summary>
public class PricingEngine : IPricingEngine
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PricingEngine> _logger;

    public PricingEngine(ApplicationDbContext context, ILogger<PricingEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PricingCalculationResult> CalculatePriceAsync(
        Guid productId,
        int quantity,
        string? region = null,
        string? customerSegment = null,
        DateTime? checkDate = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.ProductPricings)
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null, cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }

        var basePrice = product.ProductPricings
            .Where(pp => pp.IsDefault && pp.DeletedAt == null)
            .Select(pp => pp.BasePrice)
            .FirstOrDefault();

        if (basePrice == 0)
        {
            throw new InvalidOperationException($"No default pricing found for product {productId}");
        }

        var result = new PricingCalculationResult
        {
            ProductId = productId,
            Quantity = quantity,
            BasePrice = basePrice,
            Currency = "USD"
        };

        // Get applicable rules
        var rules = await GetApplicableRulesAsync(productId, quantity, region, customerSegment, checkDate, cancellationToken);
        var orderedRules = rules.OrderByDescending(r => r.Priority).ToList();

        decimal currentPrice = basePrice;
        decimal totalDiscount = 0;

        foreach (var rule in orderedRules)
        {
            var discountedPrice = rule.CalculatePrice(currentPrice, quantity);
            var discount = currentPrice - discountedPrice;

            if (discount > 0)
            {
                totalDiscount += discount;
                currentPrice = discountedPrice;
                result.AppliedRules.Add($"{rule.Name} ({rule.RuleType})");
            }
        }

        // Check for tier pricing
        var tier = await GetBestTierAsync(productId, quantity, cancellationToken);
        if (tier != null)
        {
            var tierPrice = tier.UnitPrice;
            if (tierPrice < currentPrice)
            {
                totalDiscount += (currentPrice - tierPrice);
                currentPrice = tierPrice;
                result.AppliedRules.Add($"Tier: {tier.Name}");
            }
        }

        result.TotalDiscountAmount = totalDiscount * quantity;
        result.TotalDiscountPercentage = basePrice > 0 ? (totalDiscount / basePrice) * 100 : 0;
        result.FinalUnitPrice = currentPrice;
        result.FinalTotalPrice = currentPrice * quantity;

        _logger.LogInformation(
            "Calculated price for product {ProductId}: Base={BasePrice}, Final={FinalPrice}, Discount={Discount}%",
            productId, basePrice, currentPrice, result.TotalDiscountPercentage);

        return result;
    }

    public async Task<IEnumerable<PricingRule>> GetApplicableRulesAsync(
        Guid productId,
        int quantity,
        string? region = null,
        string? customerSegment = null,
        DateTime? checkDate = null,
        CancellationToken cancellationToken = default)
    {
        var now = checkDate ?? DateTime.UtcNow;

        var query = _context.Set<PricingRule>()
            .Where(r => r.ProductId == productId && r.IsActive);

        // Date filters
        query = query.Where(r =>
            (r.StartDate == null || r.StartDate <= now) &&
            (r.EndDate == null || r.EndDate > now));

        // Quantity filters
        query = query.Where(r =>
            (r.MinQuantity == null || quantity >= r.MinQuantity) &&
            (r.MaxQuantity == null || quantity <= r.MaxQuantity));

        // Region filter
        if (!string.IsNullOrWhiteSpace(region))
        {
            query = query.Where(r => r.Region == null || r.Region == region);
        }

        // Customer segment filter
        if (!string.IsNullOrWhiteSpace(customerSegment))
        {
            query = query.Where(r => r.CustomerSegment == null || r.CustomerSegment == customerSegment);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PricingTier?> GetBestTierAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<PricingTier>()
            .Where(t => t.ProductId == productId && t.IsActive)
            .Where(t => quantity >= t.MinQuantity && (t.MaxQuantity == null || quantity <= t.MaxQuantity))
            .OrderBy(t => t.UnitPrice)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> CalculateVolumeDiscountAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var volumeRules = await _context.Set<PricingRule>()
            .Where(r => r.ProductId == productId && r.IsActive && r.RuleType == PricingRuleType.VolumeDiscount)
            .Where(r => quantity >= r.MinQuantity && (r.MaxQuantity == null || quantity <= r.MaxQuantity))
            .OrderByDescending(r => r.DiscountPercentage)
            .FirstOrDefaultAsync(cancellationToken);

        return volumeRules?.DiscountPercentage ?? 0;
    }

    public async Task<decimal> SuggestOptimalPricingAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        // Get current pricing
        var product = await _context.Products
            .Include(p => p.ProductPricings)
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null, cancellationToken);

        if (product == null) return 0;

        var currentPrice = product.ProductPricings
            .Where(pp => pp.IsDefault && pp.DeletedAt == null)
            .Select(pp => pp.BasePrice)
            .FirstOrDefault();

        // Simple market-based suggestion (in real scenario, would analyze competitor pricing, demand, etc.)
        // For now, return a suggestion within 10% of current price
        var suggestion = currentPrice * 0.95m; // Suggest 5% discount as optimal

        _logger.LogInformation(
            "Optimal pricing suggestion for product {ProductId}: Current={Current}, Suggested={Suggested}",
            productId, currentPrice, suggestion);

        return suggestion;
    }
}
