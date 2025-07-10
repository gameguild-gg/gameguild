using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for bulk deleting user profiles
/// </summary>
public class BulkDeleteUserProfilesHandler(ApplicationDbContext context, ILogger<BulkDeleteUserProfilesHandler> logger)
    : ICommandHandler<BulkDeleteUserProfilesCommand, GameGuild.Common.Result<int>>
{
    public async Task<GameGuild.Common.Result<int>> Handle(BulkDeleteUserProfilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userProfileIds = request.UserProfileIds.ToList();
            if (!userProfileIds.Any())
            {
                return GameGuild.Common.Result.Success(0);
            }

            var userProfiles = await context.Resources.OfType<UserProfile>()
                .Where(up => userProfileIds.Contains(up.Id) && up.DeletedAt == null)
                .ToListAsync(cancellationToken);

            if (!userProfiles.Any())
            {
                logger.LogWarning("No active user profiles found for bulk deletion with IDs: {UserProfileIds}", 
                    string.Join(", ", userProfileIds));
                return GameGuild.Common.Result.Success(0);
            }

            var deletedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var userProfile in userProfiles)
            {
                if (request.SoftDelete)
                {
                    userProfile.DeletedAt = now;
                    userProfile.UpdatedAt = now;
                }
                else
                {
                    context.Resources.Remove(userProfile);
                }
                deletedCount++;
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Bulk {DeleteType} deleted {Count} user profiles. Reason: {Reason}",
                request.SoftDelete ? "soft" : "hard", deletedCount, request.Reason ?? "Not specified");

            return GameGuild.Common.Result.Success(deletedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk delete of user profiles");
            return GameGuild.Common.Result.Failure<int>(
                GameGuild.Common.Error.Failure("UserProfile.BulkDeleteFailed", "Failed to bulk delete user profiles"));
        }
    }
}
