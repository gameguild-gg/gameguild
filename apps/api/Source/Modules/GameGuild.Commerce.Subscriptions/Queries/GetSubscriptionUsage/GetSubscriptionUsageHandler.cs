using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for <see cref="GetSubscriptionUsageQuery" /> (issue #1205 G-8): resolves the
///     subscription's usage statistics through the registered query service and projects
///     them onto the client-facing usage DTO with limit warnings. Returns
///     <see langword="null" /> when the subscription does not exist.
/// </summary>
public sealed class GetSubscriptionUsageHandler(ISubscriptionQueryService queryService)
    : IQueryHandler<GetSubscriptionUsageQuery, SubscriptionUsageDto?>
{
    public async Task<SubscriptionUsageDto?> Handle(
        GetSubscriptionUsageQuery request,
        CancellationToken cancellationToken)
    {
        SubscriptionUsageStatistics statistics;
        try
        {
            statistics = await queryService
                .GetUsageStatisticsAsync(request.SubscriptionId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SubscriptionNotFoundException)
        {
            return null;
        }

        var dto = new SubscriptionUsageDto
        {
            SubscriptionId = statistics.SubscriptionId,
            UsersCount = statistics.UserCount,
            MaxUsers = statistics.PlanLimits.UnlimitedUsers ? null : statistics.PlanLimits.MaxUsers,
            StorageUsedMb = statistics.StorageUsedMb,
            MaxStorageMb = statistics.PlanLimits.UnlimitedStorage ? null : statistics.PlanLimits.MaxStorageMb,
            ApiCallsThisMonth = statistics.ApiCallsThisMonth,
            MaxApiCallsPerMonth = statistics.PlanLimits.UnlimitedApiCalls ? null : statistics.PlanLimits.MaxApiCallsPerMonth
        };

        if (dto.MaxUsers.HasValue && dto.UsersCount > dto.MaxUsers.Value)
        {
            dto.IsOverLimit = true;
            dto.LimitWarnings.Add($"Users ({dto.UsersCount}) exceed the plan limit ({dto.MaxUsers.Value}).");
        }

        if (dto.MaxStorageMb.HasValue && dto.StorageUsedMb > dto.MaxStorageMb.Value)
        {
            dto.IsOverLimit = true;
            dto.LimitWarnings.Add($"Storage ({dto.StorageUsedMb} MB) exceeds the plan limit ({dto.MaxStorageMb.Value} MB).");
        }

        if (dto.MaxApiCallsPerMonth.HasValue && dto.ApiCallsThisMonth > dto.MaxApiCallsPerMonth.Value)
        {
            dto.IsOverLimit = true;
            dto.LimitWarnings.Add($"API calls ({dto.ApiCallsThisMonth}) exceed the monthly plan limit ({dto.MaxApiCallsPerMonth.Value}).");
        }

        return dto;
    }
}
