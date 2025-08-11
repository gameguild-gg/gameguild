using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handles UserProfileUpdatedEvent - manages update notifications and integrations
/// </summary>
public class UserProfileUpdatedEventHandler : IDomainEventHandler<UserProfileUpdatedEvent> {
  private readonly ILogger<UserProfileUpdatedEventHandler> _logger;

  public UserProfileUpdatedEventHandler(ILogger<UserProfileUpdatedEventHandler> logger) { _logger = logger; }

  public async Task Handle(UserProfileUpdatedEvent domainEvent, CancellationToken cancellationToken) {
    _logger.LogInformation(
      "User profile updated: {UserProfileId} for user {UserId} with {ChangeCount} changes",
      domainEvent.UserProfileId,
      domainEvent.UserId,
      domainEvent.Changes.Count
    );

    // Here you can add integrations like:
    // - Update search index with changes
    // - Send change notifications
    // - Update external systems
    // - Log audit trails
    // - Invalidate cache

    await Task.CompletedTask;
  }
}
