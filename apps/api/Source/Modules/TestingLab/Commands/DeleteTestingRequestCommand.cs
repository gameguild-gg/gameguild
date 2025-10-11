using GameGuild.CQRS;
using GameGuild.Modules.TestingLab.Entities;


namespace GameGuild.Modules.TestingLab;

public record DeleteTestingRequestCommand(Guid Id) : IRequest<bool>;
