namespace GameGuild.Modules.TestingLab;

public record DeleteTestingRequestCommand(Guid Id) : IRequest<bool>;
