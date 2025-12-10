using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get programs by difficulty </summary>
public record GetProgramsByDifficultyQuery(ProgramDifficulty Difficulty, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;
