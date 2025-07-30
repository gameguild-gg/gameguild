using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handles UserProfileRestoredEvent - manages restoration and notifications
/// </summary>
public class UserProfileRestoredEventHandler : IDomainEventHandler<UserProfileRestoredEvent> {
  private readonly ILogger<UserProfileRestoredEventHandler> _logger;

  public UserProfileRestoredEventHandler(ILogger<UserProfileRestoredEventHandler> logger) { _logger = logger; }

  public async Task Handle(UserProfileRestoredEvent domainEvent, CancellationToken cancellationToken) {
    _logger.LogInformation(
      "User profile restored: {UserProfileId} for user {UserId}",
      domainEvent.UserProfileId,
      domainEvent.UserId
    );

    // Here you can add integrations like:
    // - Re-add to search index
    // - Restore in external systems
    // - Send restoration notifications
    // - Update analytics

    await Task.CompletedTask;
  }
}
