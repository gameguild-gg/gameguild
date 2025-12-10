using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to delete a program rating </summary>
public record DeleteProgramRatingCommand(Guid ProgramId, string UserId) : ICommand<bool>;
