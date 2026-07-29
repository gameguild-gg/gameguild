using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed record TestingApplicationVoteProjection(
    Guid Id,
    Guid ReviewerId,
    TestingApplicationVoteDecision Decision,
    string? Comments,
    DateTime CreatedAt);

public sealed record TestingProjectApplicationProjection(
    Guid Id,
    Guid EventId,
    Guid ProjectId,
    Guid? ProjectVersionId,
    Guid SubmittedByUserId,
    string? PreferredAvailability,
    TestingApplicationStatus Status,
    Guid? AssignedSlotId,
    Guid? DecidedByUserId,
    string? DecisionRationale,
    DateTime? DecidedAt,
    IReadOnlyList<TestingApplicationVoteProjection> Votes);

public sealed record SubmitTestingProjectApplicationCommand(
    Guid EventId,
    Guid ProjectId,
    Guid? ProjectVersionId,
    string? PreferredAvailability) : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record WithdrawTestingProjectApplicationCommand(Guid ApplicationId)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record BeginReviewTestingProjectApplicationCommand(Guid ApplicationId)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record CastTestingApplicationVoteCommand(
    Guid ApplicationId,
    TestingApplicationVoteDecision Decision,
    string? Comments) : ICommand<Result<TestingApplicationVoteProjection>>;

public sealed record ApproveTestingProjectApplicationCommand(
    Guid ApplicationId,
    Guid SlotId,
    string? Rationale) : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record RejectTestingProjectApplicationCommand(Guid ApplicationId, string Rationale)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record WaitlistTestingProjectApplicationCommand(Guid ApplicationId, string? Rationale)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record AssignTestingProjectApplicationSlotCommand(Guid ApplicationId, Guid SlotId)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record GetTestingProjectApplicationQuery(Guid ApplicationId)
    : IQuery<Result<TestingProjectApplicationProjection>>;

public sealed record GetTestingEventApplicationsQuery(
    Guid EventId,
    TestingApplicationStatus? Status = null,
    int Skip = 0,
    int Take = 50) : IQuery<Result<IReadOnlyList<TestingProjectApplicationProjection>>>;

public sealed record GetMyTestingProjectApplicationsQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingProjectApplicationProjection>>>;
public sealed record SubmitTestingProjectApplicationRequest(
    Guid ProjectId,
    Guid? ProjectVersionId,
    string? PreferredAvailability);

public sealed record CastTestingApplicationVoteRequest(
    TestingApplicationVoteDecision Decision,
    string? Comments);

public sealed record DecideTestingProjectApplicationRequest(Guid? SlotId, string? Rationale);

public sealed record AssignTestingProjectApplicationSlotRequest(Guid SlotId);