namespace GameGuild.Modules.TestingLab;

public record GetTestingAnalyticsQuery(
  Guid? ProjectVersionId = null,
  DateTime? FromDate = null,
  DateTime? ToDate = null,
  bool IncludeTrends = true
) : GameGuild.CQRS.IRequest<TestingAnalytics>;
