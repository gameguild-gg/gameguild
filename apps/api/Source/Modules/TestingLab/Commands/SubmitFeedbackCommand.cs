using GameGuild.CQRS;
using GameGuild.Modules.TestingLab.Entities;


namespace GameGuild.Modules.TestingLab;

public record SubmitFeedbackCommand(Guid TestingRequestId, Guid UserId, string Content, FeedbackQuality? QualityRating, int? Rating, bool IsAnonymous = false) : IRequest<TestingFeedback>;
