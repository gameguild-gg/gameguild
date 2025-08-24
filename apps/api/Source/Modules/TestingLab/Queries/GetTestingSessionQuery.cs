namespace GameGuild.Modules.TestingLab.Queries;

public record GetTestingSessionQuery(Guid Id) : IRequest<TestingSession?>;
