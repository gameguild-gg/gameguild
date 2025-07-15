using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handles UserProfileDeletedEvent - manages cleanup and notifications
/// </summary>
public class UserProfileDeletedEventHandler : IDomainEventHandler<UserProfileDeletedEvent> {
  private readonly ILogger<UserProfileDeletedEventHandler> _logger;

  public UserProfileDeletedEventHandler(ILogger<UserProfileDeletedEventHandler> logger) { _logger = logger; }

  public async Task Handle(UserProfileDeletedEvent domainEvent, CancellationToken cancellationToken) {
    _logger.LogInformation(
      "User profile deleted: {UserProfileId} for user {UserId} (soft delete: {IsSoftDelete})",
      domainEvent.UserProfileId,
      domainEvent.UserId,
      domainEvent.IsSoftDelete
    );

    // Here you can add integrations like:
    // - Remove from search index
    // - Clean up external systems
    // - Send deletion notifications
    // - Archive related data
    // - Update analytics

    await Task.CompletedTask;
  }
}
