namespace GameGuild.Modules.TestingLab;

public record GetTestingRequestQuery(Guid Id) : GameGuild.CQRS.IRequest<TestingRequest?>;
