using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for getting user profile by user ID
/// </summary>
public class GetUserProfileByUserIdHandler(ApplicationDbContext context, ILogger<GetUserProfileByUserIdHandler> logger)
  : IQueryHandler<GetUserProfileByUserIdQuery, Common.Result<UserProfile?>> {
  public async Task<Common.Result<UserProfile?>> Handle(GetUserProfileByUserIdQuery request, CancellationToken cancellationToken) {
    try {
      IQueryable<UserProfile> query = context.Resources.OfType<UserProfile>()
                                             .Include(up => up.Metadata);

      if (!request.IncludeDeleted)
        query = query.Where(up => up.DeletedAt == null);
      else
        query = query.IgnoreQueryFilters();

      // UserProfile ID matches User ID for 1:1 relationship
      var userProfile = await query
                          .FirstOrDefaultAsync(up => up.Id == request.UserId, cancellationToken);

      if (userProfile == null) {
        logger.LogDebug("User profile not found for user: {UserId}", request.UserId);

        return Result.Success<UserProfile?>(null);
      }

      logger.LogDebug("Retrieved user profile for user: {UserId}", request.UserId);

      return Result.Success<UserProfile?>(userProfile);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving user profile for user {UserId}", request.UserId);

      return Result.Failure<UserProfile?>(
        Common.Error.Failure("UserProfile.QueryFailed", "Failed to retrieve user profile")
      );
    }
  }
}
