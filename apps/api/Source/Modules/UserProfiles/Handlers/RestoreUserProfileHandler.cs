using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for restoring a soft-deleted user profile </summary>
public class RestoreUserProfileHandler(IUserProfileService userProfileService, ILogger<RestoreUserProfileHandler> logger, IDomainEventPublisher eventPublisher) : ICommandHandler<RestoreUserProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RestoreUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Attempting to restore user profile {UserProfileId}", request.UserProfileId);

            // Check if the user profile exists (including soft-deleted ones)
            UserProfile? userProfile = await userProfileService.GetUserProfileByIdAsync(request.UserProfileId);

            if (userProfile == null)
            {
                logger.LogWarning("User profile {UserProfileId} not found for restoration", request.UserProfileId);

                return Result.Failure<bool>(Error.NotFound("UserProfile.NotFound", $"User profile with ID {request.UserProfileId} not found"));
            }

            if (!userProfile.IsDeleted)
            {
                logger.LogInformation("User profile {UserProfileId} is not deleted, no restoration needed", request.UserProfileId);

                return Result.Success(true);
            }

            bool restoreResult = await userProfileService.RestoreUserProfileAsync(request.UserProfileId);

            if (!restoreResult)
            {
                logger.LogError("Failed to restore user profile {UserProfileId}", request.UserProfileId);

                return Result.Failure<bool>(Error.Failure("UserProfile.RestoreFailed", "Failed to restore user profile"));
            }

            logger.LogInformation("User profile {UserProfileId} restored successfully", request.UserProfileId);

            // Publish domain event for the restoration
            await eventPublisher.PublishAsync(
                new UserProfileRestoredEvent(
                    request.UserProfileId,
                    request.UserProfileId, // UserProfile ID matches User ID in 1:1 relationship
                    DateTime.UtcNow
                ),
                cancellationToken
            );

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error restoring user profile {UserProfileId}", request.UserProfileId);

            return Result.Failure<bool>(Error.Failure("UserProfile.RestoreFailed", "Failed to restore user profile"));
        }
    }
}
