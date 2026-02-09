using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to delete a program rating </summary>
public sealed record DeleteProgramRatingCommand(Guid ProgramId, string UserId) : ICommand<bool>;
