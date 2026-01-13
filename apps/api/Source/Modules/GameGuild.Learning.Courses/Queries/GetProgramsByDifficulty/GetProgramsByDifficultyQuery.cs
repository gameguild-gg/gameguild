using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get programs by difficulty </summary>
public record GetProgramsByDifficultyQuery(ProgramDifficulty Difficulty, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;
