using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to publish a program </summary>
public record PublishProgramCommand(Guid Id) : ICommand<Program>;
