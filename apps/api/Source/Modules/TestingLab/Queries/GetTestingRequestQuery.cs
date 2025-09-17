using GameGuild.CQRS;

namespace GameGuild.Modules.TestingLab;

public record GetTestingRequestQuery(Guid Id) : IRequest<TestingRequest?>;
