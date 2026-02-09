using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record GetTestingFeedbackQuery(Guid TestingRequestId, int Skip = 0, int Take = 50, Guid? UserId = null, FeedbackQuality? MinQualityRating = null) : IRequest<IEnumerable<TestingFeedback>>;
