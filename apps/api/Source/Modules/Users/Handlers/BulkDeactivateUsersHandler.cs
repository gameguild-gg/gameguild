using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for bulk deactivating users
/// </summary>
public class BulkDeactivateUsersHandler(
  ApplicationDbContext context,
  ILogger<BulkDeactivateUsersHandler> logger,
  IMediator mediator
) : ICommandHandler<BulkDeactivateUsersCommand, BulkOperationResult> {
  public async Task<BulkOperationResult> Handle(BulkDeactivateUsersCommand request, CancellationToken cancellationToken) {
    var deactivatedUsers = new List<User>();
    var errors = new List<string>();
    var successfulCount = 0;

    foreach (var userId in request.UserIds) {
      try {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

        if (user == null) {
          errors.Add($"User with ID {userId} not found");

          continue;
        }

        if (!user.IsActive) {
          successfulCount++; // Already inactive, count as success

          continue;
        }

        user.IsActive = false;
        user.Touch();
        deactivatedUsers.Add(user);
        successfulCount++;
      }
      catch (Exception ex) {
        errors.Add($"Failed to deactivate user {userId}: {ex.Message}");
        logger.LogError(ex, "Failed to deactivate user {UserId}", userId);
      }
    }

    if (deactivatedUsers.Count != 0) {
      await context.SaveChangesAsync(cancellationToken);

      // Publish domain events for deactivated users
      foreach (var user in deactivatedUsers) await mediator.Publish(new UserDeactivatedEvent(user.Id), cancellationToken);
    }

    var result = new BulkOperationResult(request.UserIds.Count, successfulCount, errors.Count);

    foreach (var error in errors) result.AddError(error);

    logger.LogInformation(
      "Bulk deactivate completed: {Successful}/{Total} users deactivated. Reason: {Reason}",
      successfulCount,
      request.UserIds.Count,
      request.Reason ?? "Not specified"
    );

    return result;
  }
}
