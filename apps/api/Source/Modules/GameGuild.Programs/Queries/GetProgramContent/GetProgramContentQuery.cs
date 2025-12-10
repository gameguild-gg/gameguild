using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get program content </summary>
public record GetProgramContentQuery(Guid ProgramId, bool OnlyVisible = true, string? UserId = null) : IQuery<IEnumerable<ProgramContent>>;
