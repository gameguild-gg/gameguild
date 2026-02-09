using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record GetTestingAnalyticsQuery(Guid? ProjectVersionId = null, DateTime? FromDate = null, DateTime? ToDate = null, bool IncludeTrends = true) : IRequest<TestingAnalytics>;
