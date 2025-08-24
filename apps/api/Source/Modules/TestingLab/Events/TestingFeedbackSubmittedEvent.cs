using MediatR;

namespace GameGuild.Modules.TestingLab.Events;

public record TestingFeedbackSubmittedEvent(
    Guid FeedbackId,
    Guid TestingRequestId,
    Guid UserId,
    FeedbackQualityRating? QualityRating,
    DateTime SubmittedAt
) : INotification;
