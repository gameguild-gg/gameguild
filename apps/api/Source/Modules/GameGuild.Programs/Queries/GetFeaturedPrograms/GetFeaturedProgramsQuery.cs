using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get featured programs </summary>
public record GetFeaturedProgramsQuery(int Skip = 0, int Take = 10) : IQuery<IEnumerable<Program>>;
