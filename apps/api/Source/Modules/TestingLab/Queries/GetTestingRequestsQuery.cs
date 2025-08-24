using MediatR;

namespace GameGuild.Modules.TestingLab.Queries;

public record GetTestingRequestsQuery(
    int Skip = 0,
    int Take = 50,
    Guid? ProjectVersionId = null,
    TestingRequestStatus? Status = null,
    bool? IsActive = null
) : IRequest<IEnumerable<TestingRequest>>;
