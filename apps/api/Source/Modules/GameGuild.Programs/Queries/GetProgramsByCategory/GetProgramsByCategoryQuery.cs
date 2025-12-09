using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get programs by category </summary>
public record GetProgramsByCategoryQuery(ProgramCategory Category, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;
