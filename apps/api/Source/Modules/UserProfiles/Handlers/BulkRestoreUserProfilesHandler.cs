using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for bulk restoring soft-deleted user profiles
/// </summary>
public class BulkRestoreUserProfilesHandler(ApplicationDbContext context, ILogger<BulkRestoreUserProfilesHandler> logger)
  : ICommandHandler<BulkRestoreUserProfilesCommand, Common.Result<int>> {
  public async Task<Common.Result<int>> Handle(BulkRestoreUserProfilesCommand request, CancellationToken cancellationToken) {
    try {
      var userProfileIds = request.UserProfileIds.ToList();

      if (!userProfileIds.Any()) { return Result.Success(0); }

      var userProfiles = await context.Resources.OfType<UserProfile>()
                                      .IgnoreQueryFilters()
                                      .Where(up => userProfileIds.Contains(up.Id) && up.DeletedAt != null)
                                      .ToListAsync(cancellationToken);

      if (!userProfiles.Any()) {
        logger.LogWarning(
          "No deleted user profiles found for bulk restoration with IDs: {UserProfileIds}",
          string.Join(", ", userProfileIds)
        );

        return Result.Success(0);
      }

      var restoredCount = 0;
      var now = DateTime.UtcNow;

      foreach (var userProfile in userProfiles) {
        userProfile.DeletedAt = null;
        userProfile.UpdatedAt = now;
        restoredCount++;
      }

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation(
        "Bulk restored {Count} user profiles. Reason: {Reason}",
        restoredCount,
        request.Reason ?? "Not specified"
      );

      return Result.Success(restoredCount);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error during bulk restore of user profiles");

      return Result.Failure<int>(
        Common.Error.Failure("UserProfile.BulkRestoreFailed", "Failed to bulk restore user profiles")
      );
    }
  }
}
