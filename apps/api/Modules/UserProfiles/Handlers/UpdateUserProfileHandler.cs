using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for updating user profile with business logic and optimistic concurrency control
/// </summary>
public class UpdateUserProfileHandler(IUserProfileService userProfileService, ILogger<UpdateUserProfileHandler> logger, IDomainEventPublisher eventPublisher)
    : ICommandHandler<UpdateUserProfileCommand, Result<UserProfile>>
{
    public async Task<Result<UserProfile>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            UserProfile? userProfile = await userProfileService.GetUserProfileByIdAsync(request.UserProfileId);

            if (userProfile == null) return Result.Failure<UserProfile>(Error.NotFound("UserProfile.NotFound", $"User profile with ID {request.UserProfileId} not found"));

            // Track changes for notification
            var changes = new Dictionary<string, object>();

            // Update profile properties - only the ones that are provided
            if (request.DisplayName != null && userProfile.DisplayName != request.DisplayName)
            {
                changes["DisplayName"] = new { From = userProfile.DisplayName, To = request.DisplayName };
                userProfile.DisplayName = request.DisplayName;
            }

            // Only update if there are actual changes
            if (changes.Count == 0) return Result.Success(userProfile);

            UserProfile? updatedProfile = await userProfileService.UpdateUserProfileAsync(userProfile.Id, userProfile);

            if (updatedProfile == null) { return Result.Failure<UserProfile>(Error.Failure("UserProfile.UpdateFailed", "Failed to update user profile")); }

            logger.LogInformation("User profile {UserProfileId} updated successfully with {ChangeCount} changes", request.UserProfileId, changes.Count);

            // Publish domain event with changes
            await eventPublisher.PublishAsync(
                new UserProfileUpdatedEvent(
                    updatedProfile.Id,
                    updatedProfile.Id, // Assuming 1:1 relationship
                    changes,
                    updatedProfile.UpdatedAt
                ),
                cancellationToken
            );

            return Result.Success(updatedProfile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile {UserProfileId}", request.UserProfileId);

            return Result.Failure<UserProfile>(Error.Failure("UserProfile.UpdateFailed", "Failed to update user profile"));
        }
    }
}
