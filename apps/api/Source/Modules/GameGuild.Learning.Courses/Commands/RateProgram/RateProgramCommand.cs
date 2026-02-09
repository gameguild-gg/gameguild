using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to rate a program </summary>
public sealed record RateProgramCommand(Guid ProgramId, string UserId, int Rating, string? Review) : ICommand<ProgramRating>;
