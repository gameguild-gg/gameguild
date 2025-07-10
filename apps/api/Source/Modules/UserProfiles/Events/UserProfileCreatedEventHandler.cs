using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles.Events;

/// <summary>
/// Handles UserProfileCreatedEvent - manages integrations and side effects
/// </summary>
public class UserProfileCreatedEventHandler : IDomainEventHandler<UserProfileCreatedEvent> {
  private readonly ILogger<UserProfileCreatedEventHandler> _logger;

  public UserProfileCreatedEventHandler(ILogger<UserProfileCreatedEventHandler> logger) { _logger = logger; }

  public async Task Handle(UserProfileCreatedEvent domainEvent, CancellationToken cancellationToken) {
    _logger.LogInformation(
      "User profile created: {UserProfileId} for user {UserId} with display name '{DisplayName}'",
      domainEvent.UserProfileId,
      domainEvent.UserId,
      domainEvent.DisplayName
    );

    // Here you can add integrations like:
    // - Send welcome email
    // - Update search index
    // - Create user profile in external systems
    // - Send notifications to admin
    // - Update analytics

    await Task.CompletedTask;
  }
}
