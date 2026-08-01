using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed record TestingLabAnalyticsSummaryProjection(
    int Events,
    int CompletedEvents,
    int Applications,
    int ApprovedProjects,
    int RegisteredTesters,
    int AttendedTesters,
    int Feedback,
    decimal? AverageRating,
    decimal? RecommendationRate,
    int Capacity,
    decimal FillRate);

public sealed record TestingLabAnalyticsTrendProjection(
    DateTime Date,
    int Events,
    int Applications,
    int Registrations,
    int Attendance,
    int Feedback);

public sealed record TestingLabEventAnalyticsProjection(
    Guid EventId,
    string Name,
    TestingEventStatus Status,
    TestingEventMode Mode,
    DateTime StartsAt,
    int Applications,
    int ApprovedProjects,
    int RegisteredTesters,
    int AttendedTesters,
    int Feedback,
    decimal? AverageRating,
    int Capacity,
    decimal FillRate);

public sealed record TestingLabLocationAnalyticsProjection(int Total, int Active);

public sealed record TestingLabAnalyticsReportProjection(
    DateTime FromDate,
    DateTime ToDate,
    DateTime GeneratedAt,
    TestingLabAnalyticsSummaryProjection Current,
    TestingLabAnalyticsSummaryProjection? Previous,
    TestingLabLocationAnalyticsProjection Locations,
    IReadOnlyList<TestingLabAnalyticsTrendProjection> Trend,
    IReadOnlyList<TestingLabEventAnalyticsProjection> Events);

public sealed record TestingLabAnalyticsExportProjection(
    string ContentType,
    string FileName,
    byte[] Content);

public sealed record GetTestingLabAnalyticsReportQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool IncludeComparison = true) : IQuery<Result<TestingLabAnalyticsReportProjection>>;

public sealed record ExportTestingLabAnalyticsReportQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<Result<TestingLabAnalyticsExportProjection>>;
