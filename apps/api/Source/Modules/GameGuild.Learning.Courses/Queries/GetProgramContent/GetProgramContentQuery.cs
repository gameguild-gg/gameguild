using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get program content </summary>
public record GetProgramContentQuery(Guid ProgramId, bool OnlyVisible = true, string? UserId = null) : IQuery<IEnumerable<ProgramContent>>;
