using System.Text.Json.Serialization;

namespace GameGuild.TestingLab;

public enum TestingFeedbackSource
{
    Request = 0,
    Event = 1,
}

public sealed record TestingFeedbackDirectoryQuery(
    string? Search = null,
    TestingFeedbackSource? Source = null,
    Guid? EventId = null,
    Guid? RequestId = null,
    Guid? UserId = null,
    bool? Reported = null,
    FeedbackQuality? Quality = null,
    int Skip = 0,
    int Take = 50);

public sealed record TestingFeedbackDirectoryItem(
    Guid Id,
    TestingFeedbackSource Source,
    Guid? TestingRequestId,
    string? RequestTitle,
    Guid? EventId,
    string? EventName,
    Guid? ApplicationId,
    Guid? ProjectId,
    string? ProjectTitle,
    Guid? ProjectVersionId,
    string? ProjectVersion,
    Guid UserId,
    string? UserName,
    string? UserEmail,
    TestingContext TestingContext,
    int? OverallRating,
    bool? WouldRecommend,
    string FeedbackData,
    string? AdditionalNotes,
    bool IsReported,
    string? ReportReason,
    Guid? ReportedByUserId,
    DateTime? ReportedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    FeedbackQuality? QualityRating,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TestingFeedbackDirectoryPage(
    IReadOnlyList<TestingFeedbackDirectoryItem> Items,
    int TotalCount,
    int Skip,
    int Take);
