using MediatR;

namespace GameGuild.Modules.TestingLab.Queries;

public record GetTestingRequestQuery(Guid Id) : IRequest<TestingRequest?>;
