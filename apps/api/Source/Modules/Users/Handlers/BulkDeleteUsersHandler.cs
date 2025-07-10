using GameGuild.Common.Models;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for bulk deleting users
/// </summary>
public class BulkDeleteUsersHandler(
  ApplicationDbContext context,
  ILogger<BulkDeleteUsersHandler> logger,
  IMediator mediator
) : IRequestHandler<BulkDeleteUsersCommand, BulkOperationResult> {
  public async Task<BulkOperationResult> Handle(BulkDeleteUsersCommand request, CancellationToken cancellationToken) {
    var users = await context.Users
                             .Where(u => request.UserIds.Contains(u.Id))
                             .ToListAsync(cancellationToken);

    var successCount = 0;
    var errors = new List<string>();

    foreach (var user in users) {
      try {
        if (request.SoftDelete) { user.SoftDelete(); }
        else { context.Users.Remove(user); }

        // Publish domain event for each user
        await mediator.Publish(new UserDeletedEvent(user.Id, request.SoftDelete), cancellationToken);

        successCount++;
      }
      catch (Exception ex) {
        errors.Add($"Failed to delete user {user.Id}: {ex.Message}");
        logger.LogError(ex, "Failed to delete user {UserId}", user.Id);
      }
    }

    // Add errors for users that weren't found
    var foundUserIds = users.Select(u => u.Id).ToHashSet();
    var notFoundIds = request.UserIds.Where(id => !foundUserIds.Contains(id));

    foreach (var notFoundId in notFoundIds) { errors.Add($"User {notFoundId} not found"); }

    await context.SaveChangesAsync(cancellationToken);

    var failedCount = request.UserIds.Count - successCount;
    var result = new BulkOperationResult(request.UserIds.Count, successCount, failedCount);

    // Add all errors to the result
    foreach (var error in errors) { result.AddError(error); }

    logger.LogInformation(
      "Bulk delete completed: {SuccessCount}/{TotalCount} users {DeleteType}. Reason: {Reason}",
      successCount,
      request.UserIds.Count,
      request.SoftDelete ? "soft deleted" : "permanently deleted",
      request.Reason ?? "Not specified"
    );

    return result;
  }
}
