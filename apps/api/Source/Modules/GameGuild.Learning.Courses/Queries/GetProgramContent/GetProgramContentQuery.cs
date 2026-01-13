using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get program content </summary>
public record GetProgramContentQuery(Guid ProgramId, bool OnlyVisible = true, string? UserId = null) : IQuery<IEnumerable<ProgramContent>>;
