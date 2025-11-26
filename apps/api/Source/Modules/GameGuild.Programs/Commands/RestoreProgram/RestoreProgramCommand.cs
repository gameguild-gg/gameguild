using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to restore a program from archive </summary>
public record RestoreProgramCommand(Guid Id) : ICommand<Program>;
