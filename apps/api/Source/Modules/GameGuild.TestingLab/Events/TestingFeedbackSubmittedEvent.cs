using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record TestingFeedbackSubmittedEvent(Guid FeedbackId, Guid TestingRequestId, Guid UserId, FeedbackQuality? QualityRating, DateTime SubmittedAt) : INotification;
