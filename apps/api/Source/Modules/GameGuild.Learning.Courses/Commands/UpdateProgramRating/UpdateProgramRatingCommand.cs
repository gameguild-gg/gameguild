using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to update a program rating </summary>
public sealed record UpdateProgramRatingCommand(Guid ProgramId, string UserId, decimal Rating, string? Review = null) : ICommand<ProgramRating>;
