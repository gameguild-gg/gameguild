using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Common;
using GameGuild.Modules.DeveloperPortal.Entities;
using GameGuild.Modules.DeveloperPortal.Services;

namespace GameGuild.Modules.DeveloperPortal.Queries;

public record GetApiKeysByDeveloperQuery(
    Guid DeveloperId,
    bool IncludeRevoked = false
) : IRequest<Result<List<ApiKey>>>;

public record GetApiUsageStatsQuery(
    Guid DeveloperId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<Result<ApiUsageStatsDto>>;

public record GetApiUsageLogsQuery(
    Guid DeveloperId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 50
) : IRequest<Result<List<ApiUsageLog>>>;

public record GetOnboardingStatusQuery(
    Guid DeveloperId
) : IRequest<Result<DeveloperOnboarding?>>;
