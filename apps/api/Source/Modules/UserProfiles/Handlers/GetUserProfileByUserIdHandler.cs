using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for getting user profile by user ID </summary>
public class GetUserProfileByUserIdHandler(IUserProfileService userProfileService, ILogger<GetUserProfileByUserIdHandler> logger) : IQueryHandler<GetUserProfileByUserIdQuery, Result<UserProfile?>>
{
    public async Task<Result<UserProfile?>> Handle(GetUserProfileByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Getting user profile for user {UserId}", request.UserId);

            UserProfile? userProfile = await userProfileService.GetUserProfileByUserIdAsync(request.UserId);

            if (userProfile == null && !request.IncludeDeleted)
            {
                logger.LogDebug("User profile not found for user {UserId}", request.UserId);

                return Result.Success<UserProfile?>(null);
            }

            // Note: The service already handles soft-delete filtering through the repository
            // The IncludeDeleted parameter would need to be implemented in the service/repository if needed

            logger.LogDebug("Successfully retrieved user profile for user {UserId}", request.UserId);

            return Result.Success<UserProfile?>(userProfile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user profile for user {UserId}", request.UserId);

            return Result.Failure<UserProfile?>(Error.Failure("UserProfile.GetByUserIdFailed", "Failed to get user profile by user ID"));
        }
    }
}
