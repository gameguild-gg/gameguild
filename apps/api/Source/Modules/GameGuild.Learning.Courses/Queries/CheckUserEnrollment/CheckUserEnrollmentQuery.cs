using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to check if user is enrolled in program </summary>
public sealed record CheckUserEnrollmentQuery(Guid ProgramId, string UserId) : IQuery<ProgramUser?>;
