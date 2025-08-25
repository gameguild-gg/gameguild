namespace GameGuild.Modules.TestingLab;

public record GetTestingFeedbackQuery(
  Guid TestingRequestId,
  int Skip = 0,
  int Take = 50,
  Guid? UserId = null,
  FeedbackQuality? MinQualityRating = null
) : IRequest<IEnumerable<TestingFeedback>>;
