using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to update a program rating </summary>
public record UpdateProgramRatingCommand(Guid ProgramId, string UserId, decimal Rating, string? Review = null) : ICommand<ProgramRating>;
