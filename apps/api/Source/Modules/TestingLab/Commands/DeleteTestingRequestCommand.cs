using MediatR;

namespace GameGuild.Modules.TestingLab.Commands;

public record DeleteTestingRequestCommand(Guid Id) : IRequest<bool>;
