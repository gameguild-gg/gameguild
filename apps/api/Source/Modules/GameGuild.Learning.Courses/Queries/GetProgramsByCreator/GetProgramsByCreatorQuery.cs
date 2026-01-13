using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get programs by creator </summary>
public record GetProgramsByCreatorQuery(string CreatorId, int Skip = 0, int Take = 50, bool OnlyPublished = false) : IQuery<IEnumerable<Program>>;
