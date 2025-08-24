using MediatR;

namespace GameGuild.Modules.TestingLab.Queries;

public record GetTestingAnalyticsQuery(
    Guid? ProjectVersionId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool IncludeTrends = true
) : IRequest<TestingAnalytics>;
