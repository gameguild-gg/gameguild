using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for creating user profile with business logic and validation
/// </summary>
public class CreateUserProfileHandler(
    ApplicationDbContext context, 
    ILogger<CreateUserProfileHandler> logger,
    IDomainEventPublisher eventPublisher) : ICommandHandler<CreateUserProfileCommand, GameGuild.Common.Result<UserProfile>>
{
    public async Task<GameGuild.Common.Result<UserProfile>> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user profile already exists for this user
            var existingProfile = await context.Resources.OfType<UserProfile>()
                .FirstOrDefaultAsync(up => up.Id == request.UserId && up.DeletedAt == null, cancellationToken);

            if (existingProfile != null) 
            {
                return GameGuild.Common.Result.Failure<UserProfile>(
                    GameGuild.Common.Error.Conflict("UserProfile.AlreadyExists", $"User profile already exists for user {request.UserId}"));
            }

            // Create new user profile
            var userProfile = new UserProfile
            {
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
            await eventPublisher.PublishAsync(new UserProfileCreatedEvent(
                userProfile.Id,
                request.UserId,
                userProfile.DisplayName ?? string.Empty,
                userProfile.GivenName ?? string.Empty,
                userProfile.FamilyName ?? string.Empty,
                userProfile.CreatedAt
            ), cancellationToken);

            return GameGuild.Common.Result.Success(userProfile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user profile for user {UserId}", request.UserId);
            return GameGuild.Common.Result.Failure<UserProfile>(
                GameGuild.Common.Error.Failure("UserProfile.CreateFailed", "Failed to create user profile"));
        }
    }
}
