using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for getting user profile by ID
/// </summary>
public class GetUserProfileByIdHandler(ApplicationDbContext context, ILogger<GetUserProfileByIdHandler> logger)
  : IQueryHandler<GetUserProfileByIdQuery, Common.Result<UserProfile?>> {
  public async Task<Common.Result<UserProfile?>> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken) {
    try {
      IQueryable<UserProfile> query = context.Resources.OfType<UserProfile>()
                                             .Include(up => up.Metadata);

      if (!request.IncludeDeleted)
        query = query.Where(up => up.DeletedAt == null);
      else
        query = query.IgnoreQueryFilters();

      var userProfile = await query
                          .FirstOrDefaultAsync(up => up.Id == request.UserProfileId, cancellationToken);

      if (userProfile == null) {
        logger.LogDebug("User profile not found: {UserProfileId}", request.UserProfileId);

        return Result.Success<UserProfile?>(null);
      }

      logger.LogDebug("Retrieved user profile: {UserProfileId}", request.UserProfileId);

      return Result.Success<UserProfile?>(userProfile);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving user profile {UserProfileId}", request.UserProfileId);

      return Result.Failure<UserProfile?>(
        Common.Error.Failure("UserProfile.QueryFailed", "Failed to retrieve user profile")
      );
    }
  }
}
