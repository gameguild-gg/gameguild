using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for creating user profile with business logic and validation
/// </summary>
public class CreateUserProfileHandler(
  ApplicationDbContext context,
  ILogger<CreateUserProfileHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<CreateUserProfileCommand, Common.Result<UserProfile>> {
  public async Task<Common.Result<UserProfile>> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken) {
    try {
      // Check if user profile already exists for this user
      var existingProfile = await context.Resources.OfType<UserProfile>()
                                         .FirstOrDefaultAsync(up => up.Id == request.UserId && up.DeletedAt == null, cancellationToken);

      if (existingProfile != null) {
        return Result.Failure<UserProfile>(
          Common.Error.Conflict("UserProfile.AlreadyExists", $"User profile already exists for user {request.UserId}")
        );
      }

      // Create new user profile
      var userProfile = new UserProfile {
        Id = request.UserId, // UserProfile ID should match User ID for 1:1 relationship
        GivenName = request.GivenName,
        FamilyName = request.FamilyName,
        DisplayName = request.DisplayName,
        Title = request.Title ?? string.Empty,
        Description = request.Description,
      };

      context.Resources.Add(userProfile);
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("User profile created for user {UserId}", request.UserId);

      // Publish domain event
      await eventPublisher.PublishAsync(
        new UserProfileCreatedEvent(
          userProfile.Id,
          request.UserId,
          userProfile.DisplayName ?? string.Empty,
          userProfile.GivenName ?? string.Empty,
          userProfile.FamilyName ?? string.Empty,
          userProfile.CreatedAt
        ),
        cancellationToken
      );

      return Result.Success(userProfile);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error creating user profile for user {UserId}", request.UserId);

      return Result.Failure<UserProfile>(
        Common.Error.Failure("UserProfile.CreateFailed", "Failed to create user profile")
      );
    }
  }
}
