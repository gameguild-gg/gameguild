using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed record TestingEventProjection(
    Guid Id,
    string Name,
    string? Description,
    TestingEventMode Mode,
    TestingEventApprovalMode ApprovalMode,
    TestingEventStatus Status,
    Guid ManagerUserId,
    DateTime ApplicationsOpenAt,
    DateTime ApplicationsCloseAt,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresFeedback,
    TestingLearningCompletionRequirement LearningCompletionRequirement,
    Guid? CourseId,
    Guid? CohortId,
    Guid? LearningActivityId,
    Guid? TenantId,
    int SlotCount,
    int ApplicationCount);

public sealed record TestingEventSlotProjection(
    Guid Id,
    Guid EventId,
    Guid? LocationId,
    TestingEventMode Mode,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxTesters,
    int? MaxProjects,
    string? CampusName,
    string? RoomName,
    string? MeetingUrl,
    int ApprovedProjectCount,
    int RegisteredTesterCount);

public sealed record CreateTestingEventCommand(
    string Name,
    string? Description,
    TestingEventMode Mode,
    TestingEventApprovalMode ApprovalMode,
    DateTime ApplicationsOpenAt,
    DateTime ApplicationsCloseAt,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresFeedback) : ICommand<Result<TestingEventProjection>>;

public sealed record UpdateTestingEventCommand(
    Guid EventId,
    string Name,
    string? Description,
    TestingEventMode Mode,
    TestingEventApprovalMode ApprovalMode,
    DateTime ApplicationsOpenAt,
    DateTime ApplicationsCloseAt,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresFeedback) : ICommand<Result<TestingEventProjection>>;

public sealed record DeleteTestingEventCommand(Guid EventId) : ICommand<Result<bool>>;

public sealed record OpenTestingEventApplicationsCommand(Guid EventId) : ICommand<Result<TestingEventProjection>>;

public sealed record CloseTestingEventApplicationsCommand(Guid EventId) : ICommand<Result<TestingEventProjection>>;

public sealed record ConfigureTestingEventLearningCommand(
    Guid EventId,
    Guid CourseId,
    Guid? CohortId,
    Guid LearningActivityId,
    TestingLearningCompletionRequirement Requirement) : ICommand<Result<TestingEventProjection>>;

public sealed record ConfigureTestingEventLearningRequest(
    Guid CourseId,
    Guid? CohortId,
    Guid LearningActivityId,
    TestingLearningCompletionRequirement Requirement);

public sealed record CreateTestingEventSlotCommand(
    Guid EventId,
    TestingEventMode Mode,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxTesters,
    int? MaxProjects,
    string? CampusName,
    string? RoomName,
    string? MeetingUrl,
    Guid? LocationId = null) : ICommand<Result<TestingEventSlotProjection>>;

public sealed record UpdateTestingEventSlotCommand(
    Guid EventId,
    Guid SlotId,
    TestingEventMode Mode,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxTesters,
    int? MaxProjects,
    string? CampusName,
    string? RoomName,
    string? MeetingUrl,
    Guid? LocationId = null) : ICommand<Result<TestingEventSlotProjection>>;

public sealed record DeleteTestingEventSlotCommand(Guid EventId, Guid SlotId) : ICommand<Result<bool>>;

public sealed record GetTestingEventQuery(Guid EventId) : IQuery<Result<TestingEventProjection>>;

public sealed record GetTestingEventsQuery(
    TestingEventStatus? Status = null,
    int Skip = 0,
    int Take = 50) : IQuery<Result<IReadOnlyList<TestingEventProjection>>>;

public sealed record GetTestingEventSlotsQuery(Guid EventId) : IQuery<Result<IReadOnlyList<TestingEventSlotProjection>>>;

public sealed record CreateTestingEventRequest(
    string Name,
    string? Description,
    TestingEventMode Mode,
    TestingEventApprovalMode ApprovalMode,
    DateTime ApplicationsOpenAt,
    DateTime ApplicationsCloseAt,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresFeedback);

public sealed record UpdateTestingEventRequest(
    string Name,
    string? Description,
    TestingEventMode Mode,
    TestingEventApprovalMode ApprovalMode,
    DateTime ApplicationsOpenAt,
    DateTime ApplicationsCloseAt,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresFeedback);

public sealed record UpsertTestingEventSlotRequest(
    TestingEventMode Mode,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxTesters,
    int? MaxProjects,
    string? CampusName,
    string? RoomName,
    string? MeetingUrl,
    Guid? LocationId = null);