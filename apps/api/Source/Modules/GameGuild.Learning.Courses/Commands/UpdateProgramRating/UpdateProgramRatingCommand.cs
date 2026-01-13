using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to update a program rating </summary>
public record UpdateProgramRatingCommand(Guid ProgramId, string UserId, decimal Rating, string? Review = null) : ICommand<ProgramRating>;
