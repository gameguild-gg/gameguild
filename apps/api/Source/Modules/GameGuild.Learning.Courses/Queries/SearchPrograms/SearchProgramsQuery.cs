using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to search programs with advanced filtering </summary>
public sealed record SearchProgramsQuery(
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
