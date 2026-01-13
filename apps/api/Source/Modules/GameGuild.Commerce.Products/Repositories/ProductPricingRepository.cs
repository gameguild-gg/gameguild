using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for ProductPricing entities
/// </summary>
public class ProductPricingRepository(IApplicationDbContext context) : IProductPricingRepository
{
    public async Task<ProductPricing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<ProductPricing>()
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ProductPricing>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await context.Set<ProductPricing>()
            .Where(p => p.ProductId == productId && p.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        await context.Set<ProductPricing>().AddAsync(pricing, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        context.Set<ProductPricing>().Update(pricing);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        context.Set<ProductPricing>().Remove(pricing);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
