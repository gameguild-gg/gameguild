using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record SubmitFeedbackCommand(Guid TestingRequestId, Guid UserId, string Content, FeedbackQuality? QualityRating, int? Rating, bool IsAnonymous = false) : IRequest<TestingFeedback>;
