using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Handler for getting user profile statistics </summary>
public class GetUserProfileStatisticsHandler(IUserProfileService userProfileService, ILogger<GetUserProfileStatisticsHandler> logger) : IQueryHandler<GetUserProfileStatisticsQuery, Result<UserProfileStatistics>>
{
    public async Task<Result<UserProfileStatistics>> Handle(GetUserProfileStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Generating user profile statistics for TenantId={TenantId}, FromDate={FromDate}, ToDate={ToDate}, IncludeDeleted={IncludeDeleted}",
                request.TenantId, request.FromDate, request.ToDate, request.IncludeDeleted);

            UserProfileStatistics statistics = await userProfileService.GetStatisticsAsync(
                request.FromDate,
                request.ToDate,
                request.TenantId,
                request.IncludeDeleted
            );

            logger.LogDebug("Generated user profile statistics: Total={Total}, Active={Active}, New={New}",
                statistics.TotalUserProfiles, statistics.ActiveUserProfiles, statistics.NewUserProfiles);

            return Result.Success(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating user profile statistics");

            return Result.Failure<UserProfileStatistics>(Error.Failure("UserProfile.StatisticsFailed", "Failed to generate user profile statistics"));
        }
    }
}
