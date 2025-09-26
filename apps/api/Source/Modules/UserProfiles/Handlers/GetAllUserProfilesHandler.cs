using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for getting all user profiles with filtering and pagination </summary>
public class GetAllUserProfilesHandler(IUserProfileService userProfileService, ILogger<GetAllUserProfilesHandler> logger) : IQueryHandler<GetAllUserProfilesQuery, Result<IEnumerable<UserProfile>>>
{
    public async Task<Result<IEnumerable<UserProfile>>> Handle(GetAllUserProfilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userProfiles = await userProfileService.GetAllUserProfilesAsync();

            // Apply basic filtering and pagination in memory for now
            // Note: For better performance, these filters should be moved to the repository layer
            var filteredProfiles = userProfiles;

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                string searchLower = request.SearchTerm.ToLower();

                filteredProfiles = filteredProfiles.Where(up => (up.DisplayName != null && up.DisplayName.ToLower().Contains(searchLower)));
            }

            // Apply pagination
            var paginatedProfiles = filteredProfiles.OrderBy(up => up.DisplayName).Skip(request.Skip).Take(request.Take).ToList();

            logger.LogDebug(
                "Retrieved {Count} user profiles with filters: IncludeDeleted={IncludeDeleted}, TenantId={TenantId}, SearchTerm={SearchTerm}",
                paginatedProfiles.Count,
                request.IncludeDeleted,
                request.TenantId,
                request.SearchTerm
            );

            return Result.Success(paginatedProfiles.AsEnumerable());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profiles");

            return Result.Failure<IEnumerable<UserProfile>>(Error.Failure("UserProfile.QueryFailed", "Failed to retrieve user profiles"));
        }
    }
}
