using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed record TestingSlotRegistrationProjection(
    Guid Id,
    Guid EventId,
    Guid SlotId,
    Guid UserId,
    TestingSlotRegistrationStatus Status,
    int? WaitlistPosition,
    string? Notes,
    DateTime RegisteredAt,
    DateTime? PromotedAt,
    DateTime? CheckedInAt,
    DateTime? CheckedOutAt,
    DateTime? CompletedAt,
    int PendingFeedbackCount,
    QuestionnaireResponse? RegistrationResponse = null,
    DateTime? RulesAcceptedAt = null,
    DateTime? EventConfigurationFrozenAt = null);

public sealed record TestingFeedbackObligationProjection(
    Guid Id,
    Guid EventId,
    Guid SlotId,
    Guid ApplicationId,
    Guid TesterUserId,
    Guid? FeedbackId,
    TestingFeedbackObligationStatus Status,
    DateTime? FulfilledAt,
    Guid? QuestionnaireRevisionId = null);

public sealed record TestingEventFeedbackProjection(
    Guid Id,
    Guid EventId,
    Guid ApplicationId,
    Guid TesterUserId,
    string FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes,
    DateTime SubmittedAt,
    Guid? QuestionnaireRevisionId = null,
    QuestionnaireResponse? Responses = null);

public sealed record TestingEventFeedbackReviewProjection(
    Guid ObligationId,
    Guid EventId,
    Guid SlotId,
    Guid ApplicationId,
    Guid TesterUserId,
    TestingFeedbackObligationStatus Status,
    DateTime? FulfilledAt,
    TestingEventFeedbackProjection? Feedback);

public sealed record TestingParticipantDirectoryItemProjection(
    Guid RegistrationId,
    Guid EventId,
    string EventName,
    Guid SlotId,
    TestingEventMode Mode,
    DateTime StartsAt,
    DateTime EndsAt,
    string? CampusName,
    string? RoomName,
    Guid UserId,
    string UserName,
    string UserEmail,
    string? AvatarUrl,
    TestingSlotRegistrationStatus Status,
    int? WaitlistPosition,
    string? Notes,
    DateTime RegisteredAt,
    DateTime? CheckedInAt,
    DateTime? CheckedOutAt,
    DateTime? CompletedAt,
    int PendingFeedbackCount);

public sealed record TestingParticipantDirectoryProjection(
    IReadOnlyList<TestingParticipantDirectoryItemProjection> Items,
    int TotalCount,
    int RegisteredCount,
    int WaitlistedCount,
    int CheckedInCount,
    int AttendedCount,
    int CompletedCount,
    int NoShowCount);
public sealed record RegisterTestingEventSlotCommand(
    Guid SlotId,
    string? Notes,
    QuestionnaireResponse? RegistrationResponse = null,
    bool AcceptedRules = false)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record CancelTestingEventSlotRegistrationCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record CheckInTestingEventRegistrationCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record CheckOutTestingEventRegistrationCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record MarkTestingEventNoShowCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record AssignTestingProjectToTesterCommand(Guid RegistrationId, Guid ApplicationId)
    : ICommand<Result<TestingFeedbackObligationProjection>>;

public sealed record SubmitTestingEventFeedbackCommand(
    Guid ObligationId,
    string? FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes,
    Guid? QuestionnaireRevisionId = null,
    QuestionnaireResponse? Responses = null) : ICommand<Result<TestingEventFeedbackProjection>>;

public sealed record CompleteTestingEventParticipationCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record GetTestingEventSlotRegistrationsQuery(
    Guid SlotId,
    TestingSlotRegistrationStatus? Status = null) : IQuery<Result<IReadOnlyList<TestingSlotRegistrationProjection>>>;

public sealed record GetMyTestingFeedbackObligationsQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingFeedbackObligationProjection>>>;

public sealed record GetMyTestingEventFeedbackQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingEventFeedbackProjection>>>;

public sealed record GetTestingEventFeedbackQuery(Guid EventId)
    : IQuery<Result<IReadOnlyList<TestingEventFeedbackReviewProjection>>>;

public sealed record GetTestingParticipantDirectoryQuery(
    string? Search = null,
    TestingSlotRegistrationStatus? Status = null,
    int Skip = 0,
    int Take = 50) : IQuery<Result<TestingParticipantDirectoryProjection>>;
public sealed record GetMyTestingSlotRegistrationsQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingSlotRegistrationProjection>>>;

public sealed record RegisterTestingEventSlotRequest(
    string? Notes,
    QuestionnaireResponse RegistrationResponse,
    bool AcceptedRules);

public sealed record AssignTestingProjectToTesterRequest(Guid ApplicationId);

public sealed record SubmitTestingEventFeedbackRequest(
    string? FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes,
    Guid? QuestionnaireRevisionId = null,
    QuestionnaireResponse? Responses = null);
