using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using GameGuild.Modules.UserProfiles.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for deleting user profile
/// </summary>
public class DeleteUserProfileHandler(
    ApplicationDbContext context, 
    ILogger<DeleteUserProfileHandler> logger,
    IMediator mediator) : IRequestHandler<DeleteUserProfileCommand, bool>
{
    public async Task<bool> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = await context.Resources.OfType<UserProfile>()
            .FirstOrDefaultAsync(up => up.Id == request.UserProfileId, cancellationToken);

        if (userProfile == null) return false;

        if (request.SoftDelete)
        {
            if (userProfile.DeletedAt != null) return false; // Already soft deleted

            userProfile.SoftDelete();
            logger.LogInformation("User profile {UserProfileId} soft deleted", request.UserProfileId);
        }
        else
        {
            context.Resources.Remove(userProfile);
            logger.LogInformation("User profile {UserProfileId} permanently deleted", request.UserProfileId);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Publish notification
        await mediator.Publish(new UserProfileDeletedNotification
        {
            UserProfileId = userProfile.Id,
            UserId = userProfile.Id, // Assuming 1:1 relationship
            DeletedAt = DateTime.UtcNow,
            SoftDelete = request.SoftDelete,
        }, cancellationToken);

        return true;
    }
}

/// <summary>
/// Handler for restoring user profile
/// </summary>
public class RestoreUserProfileHandler(
    ApplicationDbContext context, 
    ILogger<RestoreUserProfileHandler> logger,
    IMediator mediator) : IRequestHandler<RestoreUserProfileCommand, bool>
{
    public async Task<bool> Handle(RestoreUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = await context.Resources.OfType<UserProfile>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(up => up.Id == request.UserProfileId, cancellationToken);

        if (userProfile == null || userProfile.DeletedAt == null) return false;

        userProfile.Restore();
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User profile {UserProfileId} restored", request.UserProfileId);

        // Publish notification
        await mediator.Publish(new UserProfileRestoredNotification
        {
            UserProfileId = userProfile.Id,
            UserId = userProfile.Id,
            RestoredAt = DateTime.UtcNow,
        }, cancellationToken);

        return true;
    }
}
