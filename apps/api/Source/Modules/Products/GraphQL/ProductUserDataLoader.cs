using GameGuild.Modules.Users;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Products.GraphQL;

/// <summary>
/// DataLoader interface for efficiently loading User entities for Products
/// </summary>
public interface IProductUserDataLoader : IDataLoader<Guid, User?>
{
}

/// <summary>
/// DataLoader implementation for efficiently loading User entities for Products
/// </summary>
public class ProductUserDataLoader : BatchDataLoader<Guid, User?>, IProductUserDataLoader
{
    private readonly IServiceProvider _serviceProvider;

    public ProductUserDataLoader(
        IBatchScheduler batchScheduler,
        IServiceProvider serviceProvider,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<IReadOnlyDictionary<Guid, User?>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var users = await context.Users
            .Where(u => keys.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, u => (User?)u);
    }
}
