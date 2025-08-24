namespace GameGuild.Modules.TestingLab.Commands;

public record SubmitFeedbackCommand(
  Guid TestingRequestId,
  Guid UserId,
  string Content,
  FeedbackQualityRating? QualityRating,
  int? Rating,
  bool IsAnonymous = false
) : IRequest<TestingFeedback>;
