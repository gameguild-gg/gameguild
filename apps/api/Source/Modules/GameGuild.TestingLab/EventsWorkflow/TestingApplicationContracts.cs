using GameGuild.CQRS;
using GameGuild.Projects;

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
    IReadOnlyList<TestingApplicationVoteProjection> Votes,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null,
    VersionSubmissionPolicy SubmissionVersionPolicy = VersionSubmissionPolicy.ReadyMutableUntilReview,
    TestingProjectBrief? Brief = null,
    QuestionnaireResponse? EventApplicationResponse = null,
    DateTime? RulesAcceptedAt = null,
    Guid? CurrentQuestionnaireRevisionId = null,
    QuestionnaireSchema? FeedbackQuestionnaire = null);

public sealed record CreateTestingProjectApplicationDraftCommand(Guid EventId, Guid ProjectId)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record SaveTestingProjectApplicationDraftCommand(
    Guid ApplicationId,
    Guid? ProjectVersionId,
    TestingProjectBrief? Brief,
    QuestionnaireSchema? FeedbackQuestionnaire,
    QuestionnaireResponse? EventApplicationResponse,
    bool? AcceptedRules,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record SubmitTestingProjectApplicationDraftCommand(Guid ApplicationId)
    : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record SubmitTestingProjectApplicationCommand(
    Guid EventId,
    Guid ProjectId,
    Guid ProjectVersionId,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null,
    TestingProjectBrief? Brief = null,
    QuestionnaireSchema? FeedbackQuestionnaire = null,
    QuestionnaireResponse? EventApplicationResponse = null,
    bool AcceptedRules = false) : ICommand<Result<TestingProjectApplicationProjection>>;

public sealed record UpdateTestingProjectApplicationCommand(
    Guid ApplicationId,
    Guid ProjectVersionId,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null) : ICommand<Result<TestingProjectApplicationProjection>>;

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

public sealed record TestingEventApplicationAccessProjection(
    bool CanViewApplications,
    bool CanManageApplications,
    bool CanVote);

public sealed record GetTestingEventApplicationAccessQuery(Guid EventId)
    : IQuery<Result<TestingEventApplicationAccessProjection>>;

public sealed record GetMyTestingProjectApplicationsQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingProjectApplicationProjection>>>;

public sealed record TestingApplicationTesterEligibilityProjection(
    Guid TesterUserId,
    IReadOnlyList<Guid> EligibleApplicationIds);

public sealed record GetTestingApplicationTesterEligibilityQuery(
    Guid EventId,
    IReadOnlyList<Guid> TesterUserIds)
    : IQuery<Result<IReadOnlyList<TestingApplicationTesterEligibilityProjection>>>;

public sealed record TestingApplicationReviewAssetProjection(
    Guid AssetReferenceId,
    string? DisplayName,
    string MimeType,
    string AccessUrl,
    DateTimeOffset ExpiresAt);

public sealed record TestingApplicationReviewPackageProjection(
    Guid ApplicationId,
    Guid ProjectId,
    Guid ProjectVersionId,
    string VersionNumber,
    ProjectVersionStatus VersionStatus,
    string? ReleaseNotes,
    IReadOnlyList<TestingApplicationReviewAssetProjection> Assets,
    TestingProjectBrief? Brief,
    QuestionnaireSchema? FeedbackQuestionnaire);

public sealed record GetTestingApplicationReviewPackageQuery(Guid ApplicationId)
    : IQuery<Result<TestingApplicationReviewPackageProjection>>;

public sealed record SubmitTestingProjectApplicationRequest(
    Guid ProjectId,
    Guid ProjectVersionId,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds,
    TestingProjectBrief Brief,
    QuestionnaireSchema FeedbackQuestionnaire,
    QuestionnaireResponse EventApplicationResponse,
    bool AcceptedRules);

public sealed record CreateTestingProjectApplicationDraftRequest(Guid ProjectId);

public sealed record SaveTestingProjectApplicationDraftRequest(
    Guid? ProjectVersionId,
    TestingProjectBrief? Brief,
    QuestionnaireSchema? FeedbackQuestionnaire,
    QuestionnaireResponse? EventApplicationResponse,
    bool? AcceptedRules,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null);

public sealed record UpdateTestingProjectApplicationRequest(
    Guid ProjectVersionId,
    string? PreferredAvailability,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null);

public sealed record CastTestingApplicationVoteRequest(
    TestingApplicationVoteDecision Decision,
    string? Comments);

public sealed record DecideTestingProjectApplicationRequest(Guid? SlotId, string? Rationale);

public sealed record AssignTestingProjectApplicationSlotRequest(Guid SlotId);
