using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Handler for getting all user profiles with filtering and pagination
/// </summary>
public class GetAllUserProfilesHandler(ApplicationDbContext context, ILogger<GetAllUserProfilesHandler> logger) : IQueryHandler<GetAllUserProfilesQuery, Common.Result<IEnumerable<UserProfile>>> {
  public async Task<Common.Result<IEnumerable<UserProfile>>> Handle(GetAllUserProfilesQuery request, CancellationToken cancellationToken) {
    try {
      IQueryable<UserProfile> query = context.Resources.OfType<UserProfile>().Include(up => up.Metadata);

      // Apply filters
      query = !request.IncludeDeleted ? query.Where(up => up.DeletedAt == null) : query.IgnoreQueryFilters();

      if (request.TenantId.HasValue) query = query.Where(up => EF.Property<Guid?>(up, "TenantId") == request.TenantId);

      if (!string.IsNullOrWhiteSpace(request.SearchTerm)) {
        var searchLower = request.SearchTerm.ToLower();
        query = query.Where(up =>
                              (up.GivenName != null && up.GivenName.ToLower().Contains(searchLower)) ||
                              (up.FamilyName != null && up.FamilyName.ToLower().Contains(searchLower)) ||
                              (up.DisplayName != null && up.DisplayName.ToLower().Contains(searchLower)) ||
                              (up.Title != null && up.Title.ToLower().Contains(searchLower))
        );
      }

      // Apply pagination
      var userProfiles = await query
                               .OrderBy(up => up.DisplayName)
                               .Skip(request.Skip)
                               .Take(request.Take)
                               .ToListAsync(cancellationToken);

      logger.LogDebug(
        "Retrieved {Count} user profiles with filters: IncludeDeleted={IncludeDeleted}, TenantId={TenantId}, SearchTerm={SearchTerm}",
        userProfiles.Count,
        request.IncludeDeleted,
        request.TenantId,
        request.SearchTerm
      );

      return Result.Success<IEnumerable<UserProfile>>(userProfiles);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving user profiles");

      return Result.Failure<IEnumerable<UserProfile>>(
        Common.Error.Failure("UserProfile.QueryFailed", "Failed to retrieve user profiles")
      );
    }
  }
}
