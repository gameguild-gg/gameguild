using GameGuild.Learning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Implementation of IProductProgramProvider that queries ProductProgram entities.
/// Encapsulates ProductProgram access to maintain module boundaries.
/// </summary>
public class ProductProgramProvider : IProductProgramProvider
{
    private readonly IApplicationDbContext _context;

    public ProductProgramProvider(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetProgramIdsForProductAsync(
        Guid productId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductProgram>()
            .Where(pp => pp.ProductId == productId)
            .OrderBy(pp => pp.SortOrder)
            .Select(pp => pp.ProgramId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ProductIncludesProgramAsync(
        Guid productId, 
        Guid programId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductProgram>()
            .AnyAsync(pp => pp.ProductId == productId && pp.ProgramId == programId, cancellationToken).ConfigureAwait(false);
    }
}
