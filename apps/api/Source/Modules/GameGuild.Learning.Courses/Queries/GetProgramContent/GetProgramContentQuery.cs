using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get program content </summary>
public sealed record GetProgramContentQuery(Guid ProgramId, bool OnlyVisible = true, string? UserId = null) : IQuery<IEnumerable<ProgramContent>>;
