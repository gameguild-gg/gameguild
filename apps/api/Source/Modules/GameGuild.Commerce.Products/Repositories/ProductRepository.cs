using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for Product entities
/// </summary>
public class ProductRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<Product>(context), IProductRepository
{
    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool includePricing = false,
        bool includeCreator = false)
    {
        var query = Query.Where(p => p.Id == id);

        if (includePricing)
            query = query.Include(p => p.Pricing);

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        ProductType? type = null,
        Guid? creatorId = null,
        string? searchTerm = null,
        bool? isBundle = null,
        int skip = 0,
        int take = 50,
        string sortBy = "CreatedAt",
        string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var query = Query;

        // Apply filters
        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        if (creatorId.HasValue)
            query = query.Where(p => p.CreatorId == creatorId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)));
        }

        if (isBundle.HasValue)
            query = query.Where(p => p.IsBundle == isBundle.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "name" => sortDirection.ToUpperInvariant() == "ASC"
                ? query.OrderBy(p => p.Name)
                : query.OrderByDescending(p => p.Name),
            "updatedat" => sortDirection.ToUpperInvariant() == "ASC"
                ? query.OrderBy(p => p.UpdatedAt)
                : query.OrderByDescending(p => p.UpdatedAt),
            _ => sortDirection.ToUpperInvariant() == "ASC"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip(skip)
            .Take(Math.Min(take, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(product, cancellationToken).ConfigureAwait(false);
    }

    public new Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        Entities.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        Entities.Remove(product);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
