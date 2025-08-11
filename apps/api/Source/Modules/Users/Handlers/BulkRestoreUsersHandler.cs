using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for bulk restoring users
/// </summary>
public class BulkRestoreUsersHandler(
  ApplicationDbContext context,
  ILogger<BulkRestoreUsersHandler> logger,
  IMediator mediator
) : IRequestHandler<BulkRestoreUsersCommand, BulkOperationResult> {
  public async Task<BulkOperationResult> Handle(BulkRestoreUsersCommand request, CancellationToken cancellationToken) {
    var users = await context.Users
                             .IgnoreQueryFilters()
                             .Where(u => request.UserIds.Contains(u.Id) && u.DeletedAt != null)
                             .ToListAsync(cancellationToken);

    var successCount = 0;
    var errors = new List<string>();

    foreach (var user in users) {
      try {
        user.Restore();

        // Publish domain event for each user
        await mediator.Publish(new UserRestoredEvent(user.Id), cancellationToken);

        successCount++;
      }
      catch (Exception ex) {
        errors.Add($"Failed to restore user {user.Id}: {ex.Message}");
        logger.LogError(ex, "Failed to restore user {UserId}", user.Id);
      }
    }

    // Add errors for users that weren't found or weren't deleted
    var foundUserIds = users.Select(u => u.Id).ToHashSet();
    var notFoundIds = request.UserIds.Where(id => !foundUserIds.Contains(id));

    foreach (var notFoundId in notFoundIds) errors.Add($"User {notFoundId} not found or not deleted");

    await context.SaveChangesAsync(cancellationToken);

    var failedCount = request.UserIds.Count - successCount;
    var result = new BulkOperationResult(request.UserIds.Count, successCount, failedCount);

    // Add all errors to the result
    foreach (var error in errors) result.AddError(error);

    logger.LogInformation(
      "Bulk restore completed: {SuccessCount}/{TotalCount} users restored. Reason: {Reason}",
      successCount,
      request.UserIds.Count,
      request.Reason ?? "Not specified"
    );

    return result;
  }
}
