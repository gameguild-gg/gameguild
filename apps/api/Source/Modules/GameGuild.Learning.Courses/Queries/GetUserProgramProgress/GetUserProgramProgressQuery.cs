using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get user's progress in a program </summary>
public sealed record GetUserProgramProgressQuery(Guid UserId, Guid ProgramId) : IQuery<ProgramUserProgress?>;
