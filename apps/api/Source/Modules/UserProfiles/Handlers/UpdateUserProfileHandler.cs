using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for updating user profile with business logic and optimistic concurrency control
/// </summary>
public class UpdateUserProfileHandler(
    ApplicationDbContext context, 
    ILogger<UpdateUserProfileHandler> logger,
    IDomainEventPublisher eventPublisher) : ICommandHandler<UpdateUserProfileCommand, GameGuild.Common.Result<UserProfile>>
{
    public async Task<GameGuild.Common.Result<UserProfile>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userProfile = await context.Resources.OfType<UserProfile>()
                .FirstOrDefaultAsync(up => up.Id == request.UserProfileId && up.DeletedAt == null, cancellationToken);

            if (userProfile == null) 
            {
                return GameGuild.Common.Result.Failure<UserProfile>(
                    GameGuild.Common.Error.NotFound("UserProfile.NotFound", $"User profile with ID {request.UserProfileId} not found"));
            }

            // Track changes for notification
            var changes = new Dictionary<string, object>();

            // Update profile properties - only the ones that are provided
            if (request.GivenName != null && userProfile.GivenName != request.GivenName)
            {
                changes["GivenName"] = new { From = userProfile.GivenName, To = request.GivenName };
                userProfile.GivenName = request.GivenName;
            }

            if (request.FamilyName != null && userProfile.FamilyName != request.FamilyName)
            {
                changes["FamilyName"] = new { From = userProfile.FamilyName, To = request.FamilyName };
                userProfile.FamilyName = request.FamilyName;
            }

            if (request.DisplayName != null && userProfile.DisplayName != request.DisplayName)
            {
                changes["DisplayName"] = new { From = userProfile.DisplayName, To = request.DisplayName };
                userProfile.DisplayName = request.DisplayName;
            }

            if (request.Title != null && userProfile.Title != request.Title)
            {
                changes["Title"] = new { From = userProfile.Title, To = request.Title };
                userProfile.Title = request.Title;
            }

            if (request.Description != null && userProfile.Description != request.Description)
            {
                changes["Description"] = new { From = userProfile.Description, To = request.Description };
                userProfile.Description = request.Description;
            }

            // Only save if there are actual changes
            if (changes.Any())
            {
                // Update timestamps and version
                userProfile.Touch();
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("User profile {UserProfileId} updated successfully with {ChangeCount} changes", 
                    request.UserProfileId, changes.Count);

                // Publish domain event with changes
                await eventPublisher.PublishAsync(new UserProfileUpdatedEvent(
                    userProfile.Id,
                    userProfile.Id, // Assuming 1:1 relationship
                    changes,
                    userProfile.UpdatedAt
                ), cancellationToken);
            }

            return GameGuild.Common.Result.Success(userProfile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile {UserProfileId}", request.UserProfileId);
            return GameGuild.Common.Result.Failure<UserProfile>(
                GameGuild.Common.Error.Failure("UserProfile.UpdateFailed", "Failed to update user profile"));
        }
    }
}
