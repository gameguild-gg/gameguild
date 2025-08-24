using MediatR;

namespace GameGuild.Modules.TestingLab.Queries;

public record GetTestingFeedbackQuery(
    Guid TestingRequestId,
    int Skip = 0,
    int Take = 50,
    Guid? UserId = null,
    FeedbackQualityRating? MinQualityRating = null
) : IRequest<IEnumerable<TestingFeedback>>;
