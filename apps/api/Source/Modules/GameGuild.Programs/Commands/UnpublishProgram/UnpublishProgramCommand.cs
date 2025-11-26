using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to unpublish a program </summary>
public record UnpublishProgramCommand(Guid Id) : ICommand<Program>;
