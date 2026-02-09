using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get programs by difficulty </summary>
public sealed record GetProgramsByDifficultyQuery(ProgramDifficulty Difficulty, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;
