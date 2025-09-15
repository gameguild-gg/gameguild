using GameGuild.CQRS;

using GameGuild.CQRS;

namespace GameGuild.Modules.TestingLab;

public record DeleteTestingRequestCommand(Guid Id) : IRequest<bool>;
