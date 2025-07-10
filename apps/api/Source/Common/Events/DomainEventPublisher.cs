namespace GameGuild.Common;

/// <summary>
/// Implementation of domain event publisher using MediatR
/// </summary>
public class DomainEventPublisher(ILogger<DomainEventPublisher> logger, IServiceProvider serviceProvider)
  : IDomainEventPublisher {
  public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent {
    logger.LogInformation("Publishing domain event: {EventType}", domainEvent.GetType().Name);

    using var scope = serviceProvider.CreateScope();
    var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<T>>();

    var tasks = handlers.Select(handler => handler.Handle(domainEvent, cancellationToken));
    await Task.WhenAll(tasks);

    logger.LogInformation("Successfully published domain event: {EventType}", domainEvent.GetType().Name);
  }
}
