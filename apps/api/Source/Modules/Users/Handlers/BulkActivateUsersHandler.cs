using GameGuild.Common;
using GameGuild.Common.Models;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for bulk activating users
/// </summary>
public class BulkActivateUsersHandler(
  ApplicationDbContext context,
  ILogger<BulkActivateUsersHandler> logger,
  IMediator mediator
) : ICommandHandler<BulkActivateUsersCommand, BulkOperationResult> {
  public async Task<BulkOperationResult> Handle(BulkActivateUsersCommand request, CancellationToken cancellationToken) {
    var activatedUsers = new List<User>();
    var errors = new List<string>();
    var successfulCount = 0;

    foreach (var userId in request.UserIds) {
      try {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) {
          errors.Add($"User with ID {userId} not found");
          continue;
        }

        if (user.IsActive) {
          successfulCount++; // Already active, count as success
          continue;
        }

        user.IsActive = true;
        user.Touch();
        activatedUsers.Add(user);
        successfulCount++;
      }
      catch (Exception ex) {
        errors.Add($"Failed to activate user {userId}: {ex.Message}");
        logger.LogError(ex, "Failed to activate user {UserId}", userId);
      }
    }

    if (activatedUsers.Any()) {
      await context.SaveChangesAsync(cancellationToken);

      // Publish domain events for activated users
      foreach (var user in activatedUsers) { 
        await mediator.Publish(new UserActivatedEvent(user.Id), cancellationToken); 
      }
    }

    var result = new BulkOperationResult(request.UserIds.Count, successfulCount, errors.Count);
    foreach (var error in errors) {
      result.AddError(error);
    }

    logger.LogInformation(
      "Bulk activate completed: {Successful}/{Total} users activated. Reason: {Reason}",
      successfulCount,
      request.UserIds.Count,
      request.Reason ?? "Not specified"
    );

    return result;
  }
}
