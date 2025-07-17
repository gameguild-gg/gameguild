using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for deleting user profile with business logic and validation
/// </summary>
public class DeleteUserProfileHandler(
  ApplicationDbContext context,
  ILogger<DeleteUserProfileHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<DeleteUserProfileCommand, Common.Result<bool>> {
  public async Task<Common.Result<bool>> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken) {
    try {
      // Find the user profile
      var userProfile = await context.Resources.OfType<UserProfile>()
                                     .FirstOrDefaultAsync(up => up.Id == request.UserProfileId && up.DeletedAt == null, cancellationToken);

      if (userProfile == null) {
        return Result.Failure<bool>(
          Common.ErrorMessage.PageNotFound("UserProfile.PageNotFound", $"User profile with ID {request.UserProfileId} not found")
        );
      }

      if (request.SoftDelete) {
        // Soft delete
        userProfile.DeletedAt = DateTime.UtcNow;
      }
      else {
        // Hard delete
        context.Resources.Remove(userProfile);
      }

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("User profile {UserProfileId} deleted (soft: {SoftDelete})", request.UserProfileId, request.SoftDelete);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new UserProfileDeletedEvent(
          userProfile.Id,
          userProfile.Id, // Assuming UserProfile.Id is the same as UserId for 1:1 relationship
          request.SoftDelete,
          DateTime.UtcNow
        ),
        cancellationToken
      );

      return Result.Success(true);
    }
    catch (Exception ex) {
      logger.LogError(ex, "ErrorMessage deleting user profile {UserProfileId}", request.UserProfileId);

      return Result.Failure<bool>(
        Common.ErrorMessage.Failure("UserProfile.DeleteFailed", "Failed to delete user profile")
      );
    }
  }
}
