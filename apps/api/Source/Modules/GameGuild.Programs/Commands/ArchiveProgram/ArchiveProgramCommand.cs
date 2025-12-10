using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to archive a program </summary>
public record ArchiveProgramCommand(Guid Id) : ICommand<Program>;
