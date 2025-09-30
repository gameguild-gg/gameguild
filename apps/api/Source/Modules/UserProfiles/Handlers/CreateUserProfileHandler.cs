using GameGuild.CQRS;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for creating user profile with business logic and validation </summary>
public class CreateUserProfileHandler(IUserProfileService userProfileService, IUserService userService, ILogger<CreateUserProfileHandler> logger, IDomainEventPublisher eventPublisher)
    : ICommandHandler<CreateUserProfileCommand, Result<UserProfile>>
{
    public async Task<Result<UserProfile>> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user profile already exists for this user
            UserProfile? existingProfile = await userProfileService.GetUserProfileByUserIdAsync(request.UserId);

            if (existingProfile != null) return Result.Failure<UserProfile>(Error.Conflict("UserProfile.AlreadyExists", $"User profile already exists for user {request.UserId}"));

            // Create new user profile
            var userProfile = new UserProfile
            {
                Id = request.UserId, // UserProfile ID should match User ID for 1:1 relationship
                DisplayName = request.DisplayName,
            };

            UserProfile createdProfile = await userProfileService.CreateUserProfileAsync(userProfile);

            logger.LogInformation("User profile created for user {UserId}", request.UserId);

            // Get user data for domain event
            User? user = await userService.GetUserByIdAsync(request.UserId);
            string givenName = user?.GivenName ?? string.Empty;
            string familyName = user?.FamilyName ?? string.Empty;

            // Publish domain event
            await eventPublisher.PublishAsync(
                new UserProfileCreatedEvent(createdProfile.Id, request.UserId, createdProfile.DisplayName ?? string.Empty, givenName, familyName, createdProfile.CreatedAt),
                cancellationToken
            );

            return Result.Success(createdProfile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user profile for user {UserId}", request.UserId);

            return Result.Failure<UserProfile>(Error.Failure("UserProfile.CreateFailed", "Failed to create user profile"));
        }
    }
}
