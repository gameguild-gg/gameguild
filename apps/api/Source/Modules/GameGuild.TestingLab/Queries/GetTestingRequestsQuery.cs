using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record GetTestingRequestsQuery(int Skip = 0, int Take = 50, Guid? ProjectVersionId = null, TestingRequestStatus? Status = null, bool? IsActive = null) : IRequest<IEnumerable<TestingRequest>>;
