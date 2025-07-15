using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Products;

/// <summary>
/// DataLoader interface for efficiently loading Product entities
/// </summary>
public interface IProductDataLoader : IDataLoader<Guid, Product?> { }

/// <summary>
/// DataLoader implementation for efficiently loading Product entities
/// </summary>
public class ProductDataLoader : BatchDataLoader<Guid, Product?>, IProductDataLoader {
  private readonly IServiceProvider _serviceProvider;

  public ProductDataLoader(
    IBatchScheduler batchScheduler,
    IServiceProvider serviceProvider,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options) {
    _serviceProvider = serviceProvider;
  }

  protected override async Task<IReadOnlyDictionary<Guid, Product?>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var products = await context.Products
                                .Where(p => keys.Contains(p.Id))
                                .ToListAsync(cancellationToken);

    return products.ToDictionary(p => p.Id, p => (Product?)p);
  }
}
