using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record GetTestingRequestQuery(Guid Id) : IRequest<TestingRequest?>;
