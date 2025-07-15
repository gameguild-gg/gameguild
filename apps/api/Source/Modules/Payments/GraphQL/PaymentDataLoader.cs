using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Payments;

/// <summary>
/// DataLoader interface for efficiently loading Payment entities
/// </summary>
public interface IPaymentDataLoader : IDataLoader<Guid, Payment?> { }

/// <summary>
/// DataLoader implementation for efficiently loading Payment entities
/// </summary>
public class PaymentDataLoader : BatchDataLoader<Guid, Payment?>, IPaymentDataLoader {
  private readonly IServiceProvider _serviceProvider;

  public PaymentDataLoader(
    IBatchScheduler batchScheduler,
    IServiceProvider serviceProvider,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options) {
    _serviceProvider = serviceProvider;
  }

  protected override async Task<IReadOnlyDictionary<Guid, Payment?>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var payments = await context.Payments
                                .Where(p => keys.Contains(p.Id))
                                .ToListAsync(cancellationToken);

    return payments.ToDictionary(p => p.Id, p => (Payment?)p);
  }
}
