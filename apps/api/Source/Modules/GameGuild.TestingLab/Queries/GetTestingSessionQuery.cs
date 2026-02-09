using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record GetTestingSessionQuery(Guid Id) : IRequest<TestingSession?>;
