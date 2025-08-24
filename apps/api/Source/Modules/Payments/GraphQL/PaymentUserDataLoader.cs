using GameGuild.Database;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Payments;

/// <summary>
/// DataLoader interface for efficiently loading User entities for Payments
/// </summary>
public interface IPaymentUserDataLoader : IDataLoader<Guid, User?> { }

/// <summary>
/// DataLoader implementation for efficiently loading User entities for Payments
/// </summary>
public class PaymentUserDataLoader : BatchDataLoader<Guid, User?>, IPaymentUserDataLoader {
  private readonly IServiceProvider _serviceProvider;

  public PaymentUserDataLoader(
    IBatchScheduler batchScheduler,
    IServiceProvider serviceProvider,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _serviceProvider = serviceProvider;
  }

  protected override async Task<IReadOnlyDictionary<Guid, User?>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var users = await context.Users
                             .Where(u => keys.Contains(u.Id))
                             .ToListAsync(cancellationToken);

    return users.ToDictionary(u => u.Id, u => (User?)u);
  }
}
