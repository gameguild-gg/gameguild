using GameGuild.Database;


namespace GameGuild.Common;

/// <summary>
/// Background service for processing domain events
/// </summary>
public class DomainEventProcessorService(ILogger<DomainEventProcessorService> logger, IServiceProvider serviceProvider) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    logger.LogInformation("Domain Event Processor Service started");

    while (!stoppingToken.IsCancellationRequested) {
      try {
        await ProcessPendingEvents(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Process every 5 seconds
      }
      catch (OperationCanceledException) {
        // Expected when cancellation is requested
        break;
      }
      catch (Exception ex) {
        logger.LogError(ex, "ErrorMessage occurred while processing domain events");
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait longer on error
      }
    }

    logger.LogInformation("Domain Event Processor Service stopped");
  }

  private async Task ProcessPendingEvents(CancellationToken cancellationToken) {
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Get entities with pending domain events
    var entitiesWithEvents = dbContext.ChangeTracker.Entries()
                                      .Where(e => e.Entity is IHasDomainEvents)
                                      .Select(e => e.Entity as IHasDomainEvents)
                                      .Where(e => e != null && e.DomainEvents.Any())
                                      .ToList();

    if (entitiesWithEvents.Count == 0) return;

    var domainEventPublisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

    foreach (var entity in entitiesWithEvents) {
      var events = new List<IDomainEvent>();

      if (entity != null) {
        events.AddRange(entity.DomainEvents);
        entity.ClearDomainEvents();
      }

      foreach (var domainEvent in events) await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
