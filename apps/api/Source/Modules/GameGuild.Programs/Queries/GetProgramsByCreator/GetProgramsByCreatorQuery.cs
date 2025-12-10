using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get programs by creator </summary>
public record GetProgramsByCreatorQuery(string CreatorId, int Skip = 0, int Take = 50, bool OnlyPublished = false) : IQuery<IEnumerable<Program>>;
