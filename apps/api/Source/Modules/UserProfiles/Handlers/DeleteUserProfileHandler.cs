using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for deleting user profile with business logic and validation </summary>
public class DeleteUserProfileHandler(IUserProfileService userProfileService, ILogger<DeleteUserProfileHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<DeleteUserProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the user profile
            UserProfile? userProfile = await userProfileService.GetUserProfileByIdAsync(request.UserProfileId);

            if (userProfile == null) { return Result.Failure<bool>(Error.NotFound("UserProfile.NotFound", $"User profile with ID {request.UserProfileId} not found")); }

            bool deleteResult;

            if (request.SoftDelete)
            {
                // Soft delete
                deleteResult = await userProfileService.SoftDeleteUserProfileAsync(request.UserProfileId);
            }
            else
            {
                // Hard delete
                deleteResult = await userProfileService.DeleteUserProfileAsync(request.UserProfileId);
            }

            if (!deleteResult) { return Result.Failure<bool>(Error.Failure("UserProfile.DeleteFailed", "Failed to delete user profile")); }

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user profile {UserProfileId}", request.UserProfileId);

            return Result.Failure<bool>(Error.Failure("UserProfile.DeleteFailed", "Failed to delete user profile"));
        }
    }
}
