using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using GameGuild.Modules.UserProfiles.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for creating user profile with business logic and validation
/// </summary>
public class CreateUserProfileHandler(
    ApplicationDbContext context, 
    ILogger<CreateUserProfileHandler> logger,
    IMediator mediator) : IRequestHandler<CreateUserProfileCommand, UserProfile>
{
    public async Task<UserProfile> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // Check if user profile already exists for this user
        var existingProfile = await context.Resources.OfType<UserProfile>()
            .FirstOrDefaultAsync(up => up.Id == request.UserId && up.DeletedAt == null, cancellationToken);

        if (existingProfile != null) throw new InvalidOperationException($"User profile already exists for user {request.UserId}");

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

        // Publish notification
        await mediator.Publish(new UserProfileCreatedNotification
        {
            UserProfileId = userProfile.Id,
            UserId = request.UserId,
            CreatedAt = userProfile.CreatedAt,
        }, cancellationToken);

        return userProfile;
    }
}
