using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get programs by category </summary>
public record GetProgramsByCategoryQuery(ProgramCategory Category, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;
