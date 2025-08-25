namespace GameGuild.Modules.TestingLab;

public record GetTestingSessionQuery(Guid Id) : IRequest<TestingSession?>;
