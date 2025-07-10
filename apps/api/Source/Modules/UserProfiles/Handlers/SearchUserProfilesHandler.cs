using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Entities;
using GameGuild.Modules.UserProfiles.Queries;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for searching user profiles with advanced filtering
/// </summary>
public class SearchUserProfilesHandler(ApplicationDbContext context, ILogger<SearchUserProfilesHandler> logger)
  : IQueryHandler<SearchUserProfilesQuery, Common.Result<IEnumerable<UserProfile>>> {
  public async Task<Common.Result<IEnumerable<UserProfile>>> Handle(SearchUserProfilesQuery request, CancellationToken cancellationToken) {
    try {
      IQueryable<UserProfile> query = context.Resources.OfType<UserProfile>()
                                             .Include(up => up.Metadata);

      // Apply deletion filter
      if (!request.IncludeDeleted)
        query = query.Where(up => up.DeletedAt == null);
      else
        query = query.IgnoreQueryFilters();

      // Apply tenant filter
      if (request.TenantId.HasValue) query = query.Where(up => EF.Property<Guid?>(up, "TenantId") == request.TenantId);

      // Apply search term filter
      if (!string.IsNullOrWhiteSpace(request.SearchTerm)) {
        var searchLower = request.SearchTerm.ToLower();
        query = query.Where(up =>
                              (up.GivenName != null && up.GivenName.ToLower().Contains(searchLower)) ||
                              (up.FamilyName != null && up.FamilyName.ToLower().Contains(searchLower)) ||
                              (up.DisplayName != null && up.DisplayName.ToLower().Contains(searchLower)) ||
                              (up.Title != null && up.Title.ToLower().Contains(searchLower))
        );
      }

      // Apply date filters
      if (request.CreatedAfter.HasValue) query = query.Where(up => up.CreatedAt >= request.CreatedAfter.Value);

      if (request.CreatedBefore.HasValue) query = query.Where(up => up.CreatedAt <= request.CreatedBefore.Value);

      if (request.UpdatedAfter.HasValue) query = query.Where(up => up.UpdatedAt >= request.UpdatedAfter.Value);

      if (request.UpdatedBefore.HasValue) query = query.Where(up => up.UpdatedAt <= request.UpdatedBefore.Value);

      // Apply specific field filters
      if (!string.IsNullOrWhiteSpace(request.GivenName)) query = query.Where(up => up.GivenName != null && up.GivenName.ToLower().Contains(request.GivenName.ToLower()));

      if (!string.IsNullOrWhiteSpace(request.FamilyName)) query = query.Where(up => up.FamilyName != null && up.FamilyName.ToLower().Contains(request.FamilyName.ToLower()));

      if (!string.IsNullOrWhiteSpace(request.DisplayName)) query = query.Where(up => up.DisplayName != null && up.DisplayName.ToLower().Contains(request.DisplayName.ToLower()));

      if (!string.IsNullOrWhiteSpace(request.Title)) query = query.Where(up => up.Title != null && up.Title.ToLower().Contains(request.Title.ToLower()));

      // Apply sorting
      query = request.SortBy switch {
        UserProfileSortField.CreatedAt => request.SortDirection == SortDirection.Ascending
                                            ? query.OrderBy(up => up.CreatedAt)
                                            : query.OrderByDescending(up => up.CreatedAt),
        UserProfileSortField.UpdatedAt => request.SortDirection == SortDirection.Ascending
                                            ? query.OrderBy(up => up.UpdatedAt)
                                            : query.OrderByDescending(up => up.UpdatedAt),
        UserProfileSortField.DisplayName => request.SortDirection == SortDirection.Ascending
                                              ? query.OrderBy(up => up.DisplayName)
                                              : query.OrderByDescending(up => up.DisplayName),
        UserProfileSortField.GivenName => request.SortDirection == SortDirection.Ascending
                                            ? query.OrderBy(up => up.GivenName)
                                            : query.OrderByDescending(up => up.GivenName),
        UserProfileSortField.FamilyName => request.SortDirection == SortDirection.Ascending
                                             ? query.OrderBy(up => up.FamilyName)
                                             : query.OrderByDescending(up => up.FamilyName),
        UserProfileSortField.Title => request.SortDirection == SortDirection.Ascending
                                        ? query.OrderBy(up => up.Title)
                                        : query.OrderByDescending(up => up.Title),
        _ => query.OrderByDescending(up => up.UpdatedAt)
      };

      // Apply pagination
      var userProfiles = await query
                               .Skip(request.Skip)
                               .Take(request.Take)
                               .ToListAsync(cancellationToken);

      logger.LogDebug("Search returned {Count} user profiles with advanced filters", userProfiles.Count);

      return Result.Success<IEnumerable<UserProfile>>(userProfiles);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error searching user profiles");

      return Result.Failure<IEnumerable<UserProfile>>(
        Common.Error.Failure("UserProfile.SearchFailed", "Failed to search user profiles")
      );
    }
  }
}
