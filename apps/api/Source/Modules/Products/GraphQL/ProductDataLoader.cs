using GameGuild.Database;
using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.GraphQL;

/// <summary> DataLoader interface for efficiently loading Product entities </summary>
public interface IProductDataLoader : IDataLoader<Guid, ProductEntity?> { }

/// <summary> DataLoader implementation for efficiently loading Product entities </summary>
public class ProductDataLoader : BatchDataLoader<Guid, ProductEntity?>, IProductDataLoader {
  private readonly IServiceProvider _serviceProvider;

  public ProductDataLoader(IBatchScheduler batchScheduler, IServiceProvider serviceProvider, DataLoaderOptions? options = null) : base(batchScheduler, options ?? new DataLoaderOptions()) { _serviceProvider = serviceProvider; }

  protected override async Task<IReadOnlyDictionary<Guid, ProductEntity?>> LoadBatchAsync(IReadOnlyList<Guid> keys, CancellationToken cancellationToken) {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var products = await context.Products.Where(p => keys.Contains(p.Id)).ToListAsync(cancellationToken);

    return products.ToDictionary(p => p.Id, p => (ProductEntity?)p);
  }
}
