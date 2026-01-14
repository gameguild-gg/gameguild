using GameGuild.Abstractions;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for ProductPricing entities
/// </summary>
public class ProductPricingRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<ProductPricing>(context), IProductPricingRepository
{
    public async Task<ProductPricing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ProductPricing>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.ProductId == productId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(pricing, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        Entities.Update(pricing);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProductPricing pricing, CancellationToken cancellationToken = default)
    {
        Entities.Remove(pricing);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
