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
    int PendingFeedbackCount);

public sealed record TestingFeedbackObligationProjection(
    Guid Id,
    Guid EventId,
    Guid SlotId,
    Guid ApplicationId,
    Guid TesterUserId,
    Guid? FeedbackId,
    TestingFeedbackObligationStatus Status,
    DateTime? FulfilledAt);

public sealed record TestingEventFeedbackProjection(
    Guid Id,
    Guid EventId,
    Guid ApplicationId,
    Guid TesterUserId,
    string FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes,
    DateTime SubmittedAt);

public sealed record RegisterTestingEventSlotCommand(Guid SlotId, string? Notes)
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
    string FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes) : ICommand<Result<TestingEventFeedbackProjection>>;

public sealed record CompleteTestingEventParticipationCommand(Guid RegistrationId)
    : ICommand<Result<TestingSlotRegistrationProjection>>;

public sealed record GetTestingEventSlotRegistrationsQuery(
    Guid SlotId,
    TestingSlotRegistrationStatus? Status = null) : IQuery<Result<IReadOnlyList<TestingSlotRegistrationProjection>>>;

public sealed record GetMyTestingFeedbackObligationsQuery(Guid? EventId = null)
    : IQuery<Result<IReadOnlyList<TestingFeedbackObligationProjection>>>;

public sealed record RegisterTestingEventSlotRequest(string? Notes);

public sealed record AssignTestingProjectToTesterRequest(Guid ApplicationId);

public sealed record SubmitTestingEventFeedbackRequest(
    string FeedbackData,
    int? OverallRating,
    bool? WouldRecommend,
    string? AdditionalNotes);
