using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to remove content from a program </summary>
public record RemoveProgramContentCommand(Guid ProgramId, Guid ContentId) : ICommand<bool>;
