using GameGuild.CQRS;
using GameGuild.Modules.TestingLab.Entities;


namespace GameGuild.Modules.TestingLab;

public record TestingFeedbackSubmittedEvent(Guid FeedbackId, Guid TestingRequestId, Guid UserId, FeedbackQuality? QualityRating, DateTime SubmittedAt) : INotification;
