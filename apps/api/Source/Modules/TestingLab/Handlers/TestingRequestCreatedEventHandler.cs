using MediatR;

namespace GameGuild.Modules.TestingLab.Handlers;

public class TestingRequestCreatedEventHandler : INotificationHandler<TestingRequestCreatedEvent>
{
    private readonly ILogger<TestingRequestCreatedEventHandler> _logger;

    public TestingRequestCreatedEventHandler(ILogger<TestingRequestCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TestingRequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing request created: {TestingRequestId} - {Title}", 
            notification.TestingRequestId, notification.Title);

        // Here you could add additional logic like:
        // - Sending notifications to potential testers
        // - Creating audit logs
        // - Integrating with external systems
        // - Updating analytics/metrics

        await Task.CompletedTask;
    }
}
