namespace GameGuild.Modules.TestingLab;

public record GetTestingSessionQuery(Guid Id) : GameGuild.CQRS.IRequest<TestingSession?>;
