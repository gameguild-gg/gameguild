using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to search programs with advanced filtering </summary>
public record SearchProgramsQuery(
  string SearchTerm,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  float? MinEstimatedHours = null,
  float? MaxEstimatedHours = null,
  decimal? MinRating = null,
  bool AvailableForEnrollment = false,
  int Skip = 0,
  int Take = 50
) : IQuery<IEnumerable<Program>>;
