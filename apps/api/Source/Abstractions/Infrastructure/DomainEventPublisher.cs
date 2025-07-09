using GameGuild.Common.Abstractions;


namespace GameGuild.Abstractions.Infrastructure;

/// <summary>
/// Service for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of domain event publisher using MediatR
/// </summary>
public class DomainEventPublisher(ILogger<DomainEventPublisher> logger, IServiceProvider serviceProvider) 
    : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Publishing domain event: {EventType}", domainEvent.GetType().Name);

        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<IDomainEvent>>();

        var tasks = handlers.Select(handler => handler.Handle(domainEvent, cancellationToken));
        await Task.WhenAll(tasks);

        logger.LogInformation("Successfully published domain event: {EventType}", domainEvent.GetType().Name);
    }
}
