using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record DeleteTestingRequestCommand(Guid Id) : IRequest<bool>;
